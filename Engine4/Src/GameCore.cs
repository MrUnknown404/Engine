using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Engine4.Client.Rendering;
using Engine4.IO;
using Engine4.Utility;
using Engine4.Utility.Compatability;
using Engine4.Utility.Exceptions;
using Engine4.Utility.Extensions;
using Engine4.Utility.Versions;
using NLog;

namespace Engine4;

// TODO print engine/game details

public abstract class GameCore {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public string Name { get; }
	public IPackableVersion Version { get; }

	public ConsoleRenderer? ConsoleRenderer {
		get;
		set {
			if (field == value) { return; }
			field?.Cleanup();
			field = value;
			field?.InternalSetup();
		}
	}
	/// <summary> The amount of updates per second to aim for </summary>
	/// <exception cref="Engine4Exception"> Thrown if value was set to zero </exception>
	public uint TargetUps { get; init => field = TargetUps != 0 ? value : throw new Engine4Exception($"{nameof(TargetUps)} must be above zero"); } = 60;
	/// <summary> The amount of frames per second to aim for. If zero, framerate will be uncapped </summary>
	public uint TargetFps { get; init; }
	/// <summary> The maximum amount of frames to skip while updating before rendering anyway. Set to zero to disable </summary>
	public byte MaxFrameSkip { get; init; } = 5;

	public ulong UpdateCount { get; private set; }
	public ulong FrameCount { get; private set; }

	public PerformanceMonitor? PerformanceMonitor { get; init; }

	// lifecycle
	public bool IsRunning { get; private set; }
	private bool shouldShutdown;

	protected abstract Action? PollEvents { get; }

	/// <summary> Called after internal have been set up but before <see cref="SetupGame"/> </summary>
	public event Action? OnSetupStartEvent;
	/// <summary> Called when all setup is done </summary>
	public event Action? OnSetupDoneEvent;
	/// <summary> Called on shutdown before anything is cleaned up </summary>
	public event Action? OnShutdownEvent;

	public event RequestShutdownDelegate? RequestShutdownEvent;

	protected GameCore(string name, IPackableVersion version) {
		Name = name;
		Version = version;
	}

	public void Start(string[] args, StartupSettings settings) {
		// initial setup. logging is not set up yet
		InitialSetup(settings); // logging exists beyond this point

		Logger.Info("Engine starting...");
		Engine4.PrintStartup();

		// setup
		Logger.Debug("Setting up internals...");
		SetupInternals(settings);

		Logger.Trace("Processing args...");
		ProcessArgs(args);

		Logger.Trace($"Invoking {OnSetupStartEvent.GetInvocationListCount()} {nameof(OnSetupStartEvent)}s...");
		OnSetupStartEvent?.Invoke();

		Logger.Debug("Setting up game...");
		SetupGame();

		Logger.Trace($"Invoking {OnSetupDoneEvent.GetInvocationListCount()} {nameof(OnSetupDoneEvent)}s...");
		OnSetupDoneEvent?.Invoke();

		Logger.Info("Setup done!");

		// loop
		Logger.Debug("Entering gameloop");
		GameLoop();

		// exit
		Shutdown();
	}

	private void InitialSetup(StartupSettings startupSettings) {
		Thread.CurrentThread.Name = startupSettings.MainThreadName;
		LoggerH.Setup(startupSettings.LoggingSettings);
		Logger.Info("Hello World!");

		Logger.Trace("Setting up OS compatibility...");
#if OS_WINDOWS
		Windows.Setup();
#elif OS_LINUX
		Linux.Setup();
#endif
	}

	protected virtual void ProcessArgs(string[] args) {
		// TODO process args
	}

	protected virtual void SetupInternals(StartupSettings settings) {
		Logger.Debug("Startup Settings:");
		settings.PrintValues();
	}

	protected abstract void SetupGame();
	protected abstract void Update();
	protected abstract void Render(float delta);

	private void GameLoop() {
		IsRunning = true;

		ulong ticksPerUpdate = (ulong)(1f / TargetUps * Stopwatch.Frequency);
		ulong ticksPerFrame = (ulong)(1f / TargetFps * Stopwatch.Frequency);
		ulong updateAccumulator = ticksPerUpdate;
		ulong frameAccumulator = 0;
		long currentTime = Stopwatch.GetTimestamp();

		while (IsRunning) {
			// check/poll/check again
			if (shouldShutdown) { break; }
			PollEvents?.Invoke();
			if (shouldShutdown) { break; }

			// timing
			long lastTime = currentTime;
			currentTime = Stopwatch.GetTimestamp();

			// TODO can i find a way to do this without checking every frame? should i even care about the first loop time difference?
			ulong timeDifference = UpdateCount == 0 ? 0 : (ulong)(currentTime - lastTime); // ignore the first loop.

			PerformanceMonitor?.AddTime(timeDifference);

			// update
			updateAccumulator += timeDifference;

			int frameSkip = 0;
			while (updateAccumulator >= ticksPerUpdate && (MaxFrameSkip == 0 || frameSkip < MaxFrameSkip)) {
				if (frameSkip > 0) { Logger.Warn("frame skipping..."); } // debug

				if (PerformanceMonitor != null) {
					PerformanceMonitor.TimeUpdate(InternalUpdate); // calls update & times it
				} else { InternalUpdate(); }

				updateAccumulator -= ticksPerUpdate;
				UpdateCount++;
				PerformanceMonitor?.IncrementUpdateCounter();
				frameSkip++;

				if (MaxFrameSkip != 0 && frameSkip >= MaxFrameSkip) { Logger.Warn($"FrameSkip hit max ({MaxFrameSkip}). Rendering anyways..."); }
			}

			// render
			if (TargetFps != 0) { // if frame limiting
				frameAccumulator += timeDifference;

				if (frameAccumulator >= ticksPerFrame) {
					frameAccumulator -= ticksPerFrame; // continue
				} else { continue; } // waste time
			}

			float delta = 1 - (float)(ticksPerUpdate - updateAccumulator) / ticksPerUpdate; // convert to 0-1

			if (PerformanceMonitor != null) {
				PerformanceMonitor.TimeRender(InternalRender, delta); // calls render & times it
			} else { InternalRender(delta); }

			FrameCount++;
			PerformanceMonitor?.IncrementFrameCounter();
		}

		IsRunning = false;
	}

	protected virtual void InternalUpdate() {
		Update(); //
	}

	protected virtual void InternalRender(float delta) {
		Render(delta);

		ConsoleRenderer?.InternalRender(delta); // console may depend on other renderers so it needs to render last
	}

	public void RequestShutdown(bool force) {
		if (force) {
			this.shouldShutdown = true;
			return;
		}

		bool shouldShutdown = true;
		RequestShutdownEvent?.Invoke(ref shouldShutdown);
		if (shouldShutdown) { this.shouldShutdown = true; }
	}

	// TODO make sure this is called on an unhandled exit (ie, exceptions). see if this is helpful: https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/sigterm-signal-handler
	[DoesNotReturn]
	private void Shutdown() {
		Logger.Info("Shutting down...");

		Logger.Trace($"Invoking {nameof(OnShutdownEvent)}s...");
		OnShutdownEvent?.Invoke();

		Logger.Debug("Cleaning up everything...");
		Logger.Trace("Cleaning up the game...");
		Cleanup();

		Logger.Trace("Cleaning up internals...");
		InternalCleanup();

		Logger.Info("Logger shutting down... Goodbye!");
		LoggerH.Shutdown();

		Environment.Exit(0);
	}

	protected virtual void InternalCleanup() {
		ConsoleRenderer?.Cleanup();

#if OS_WINDOWS
		Windows.Cleanup();
#elif OS_LINUX
		Linux.Cleanup();
#endif
	}

	protected abstract void Cleanup();

	public delegate bool RequestShutdownDelegate(ref bool shouldShutdown);
}