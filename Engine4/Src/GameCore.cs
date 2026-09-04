using System.Diagnostics.CodeAnalysis;
using Engine4.IO;
using Engine4.Utility.Versions;
using NLog;

namespace Engine4;

// TODO print engine/game details

public abstract class GameCore {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public string Name { get; }
	public IPackableVersion Version { get; }

	public ushort TargetFps { get; init; }
	public ushort TargetUps { get; init; }
	public byte MaxFrameSkip { get; init; } = 5;

	public ulong UpdateCount { get; private set; }

	// lifecycle
	public bool IsRunning { get; private set; }
	private bool shouldShutdown;

	protected abstract Action? PollEvents { get; }

	/// <summary> Called after internal have been set up but before <see cref="SetupGame"/> </summary>
	public event Action? OnSetupStartEvent;
	/// <summary> Called when all setup is done </summary>
	public event Action? OnSetupDoneEvent;
	public event Action? OnShutdownEvent;

	public event RequestShutdownDelegate? RequestShutdownEvent;

	protected GameCore(string name, IPackableVersion version) {
		Name = name;
		Version = version;
	}

	public void Start(string[] args, StartupSettings settings) {
		// initial setup. logging is not set up yet
		InitialSetup(settings); // logging exists beyond this point

		Logger.Info("Hello World!");
		Logger.Info("Engine starting...");

		Logger.Trace("Processing args...");
		ProcessArgs(args);

		// setup
		Logger.Debug("Setting up internals...");
		SetupInternals(settings);

		Logger.Trace($"Invoking {nameof(OnSetupStartEvent)}s...");
		OnSetupStartEvent?.Invoke();

		Logger.Debug("Setting up game...");
		SetupGame();

		Logger.Trace($"Invoking {nameof(OnSetupDoneEvent)}s...");
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
	}

	protected virtual void ProcessArgs(string[] args) {
		// TODO
	}

	protected virtual void SetupInternals(StartupSettings settings) {
		// TODO
	}

	protected abstract void SetupGame();
	protected abstract void Update();
	protected abstract void Render(float delta);

	private void GameLoop() {
		Logger.Trace("Starting gameloop...");

		IsRunning = true;
		while (IsRunning) {
			PollEvents?.Invoke();

			if (shouldShutdown) { break; }

			InternalUpdate();
			Update();
			UpdateCount++;

			float delta = 0; // TODO
			Render(delta);

			Thread.Sleep(1); // TODO remove
		}

		IsRunning = false;
	}

	protected virtual void InternalUpdate() {
		// TODO
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

		// TODO exit
		Environment.Exit(0);
	}

	protected virtual void InternalCleanup() {
		// TODO
	}

	protected abstract void Cleanup();

	public delegate bool RequestShutdownDelegate(ref bool shouldShutdown);
}