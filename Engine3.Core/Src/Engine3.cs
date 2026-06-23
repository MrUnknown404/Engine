using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Core.Debug;
using Engine3.Core.Graphics;
using Engine3.Core.Utility;
using Engine3.Core.Utility.Compatability;
using Engine3.Core.Utility.Exceptions;
using Engine3.Core.Utility.Versions;
using JetBrains.Annotations;
using NLog;

namespace Engine3.Core;

[MustDisposeResource]
public abstract class Engine3 : IDisposable {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const string Name = nameof(Engine3);
	public const bool Debug =
#if DEBUG
			true;
#else
			false;
#endif

	[field: MaybeNull]
	public static Engine3 Engine { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(Engine)} too early. {nameof(Start)}() must be called first"); private set; }

	[field: MaybeNull]
	public EngineGame Instance { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(Instance)} too early. {nameof(Start)}() must be called first"); private set; }

	public Assembly Assembly { get; }
	public Version4Interweaved Version { get; } = new(0, 0, 0);
	public GraphicsApi GraphicsApi { get; }
	public string MainThreadName { get; init; } = "Main";

	public PerformanceMonitor PerformanceMonitor { get; init; } = new();

	/// <summary> The amount of updates per second to aim for </summary>
	/// <exception cref="Engine3Exception"> Thrown if value was set to zero </exception>
	public ushort TargetUps { get; init => field = TargetUps != 0 ? value : throw new Engine3Exception($"{nameof(TargetUps)} must be above zero"); } = 60;
	/// <summary> The amount of frames per second to aim for. If zero, framerate will be uncapped </summary>
	public ushort TargetFps { get; init; }
	/// <summary> The maximum amount of frames to skip while updating before rendering anyway. Set to zero to disable </summary>
	public byte MaxFrameSkip { get; init; } = 5;

	public ulong UpdateIndex { get; private set; }
	public ulong FrameIndex { get; private set; }

	public bool WasInitialized { get; private set; }
	public bool WasDestroyed { get; private set; }

	private bool shouldRunGameLoop = true;

	private readonly List<Renderer> renderers = new();
	private readonly Queue<Renderer> renderersCloseQueue = new();

	public event OnConsoleGraphicsSetupDoneDelegate? OnConsoleGraphicsSetupDoneEvent;
	public event OnOpenGLGraphicsSetupDoneDelegate? OnOpenGLGraphicsSetupDoneEvent;
	public event OnVulkanGraphicsSetupDoneDelegate? OnVulkanGraphicsSetupDoneEvent;

	public event OnInitializeDelegate? OnInitializeEvent;
	public event OnStartDelegate? OnStartEvent;
	public event OnShutdownDelegate? OnShutdownEvent;

	protected Engine3(Assembly assembly, GraphicsApi graphicsApi) {
		Assembly = assembly;
		GraphicsApi = graphicsApi;
	}

	public void Start<T>(T instance) where T : EngineGame {
		if (WasInitialized) { throw new Engine3Exception($"Cannot call {nameof(Start)} twice"); }

		Engine = this;
		Thread.CurrentThread.Name = MainThreadName;

		LoggerH.Setup();
		Logger.Debug("Finished setting up NLog. Hello World!");

		// os compatability
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Windows.Setup(); } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Linux.Setup(); } else {
			Logger.Warn($"Unknown OS: {RuntimeInformation.OSDescription}");
		}

		// TODO look into https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/sigterm-signal-handler

		PrintSystemInfo();
		PrintEngineSettings();

		Logger.Info("Setting up engine...");

#if DEBUG
		Logger.Debug("Writing dumps to file outputs...");
		StructLayoutDumper.WriteDumpsToOutput();
#endif

		if (GraphicsApi != GraphicsApi.None) {
			Logger.Debug("Setting up pre graphics...");
			SetupPreGraphics();

			Logger.Info("Setting up graphics...");
			switch (GraphicsApi) {
				case GraphicsApi.None: throw new UnreachableException();
				case GraphicsApi.Console:
					SetupConsoleGraphics(instance);
					OnConsoleGraphicsSetupDoneEvent?.Invoke();
					break;
				case GraphicsApi.Vulkan:
					SetupVulkanGraphics(instance);
					OnVulkanGraphicsSetupDoneEvent?.Invoke();
					break;
				case GraphicsApi.OpenGL:
					SetupOpenGLGraphics(instance);
					OnOpenGLGraphicsSetupDoneEvent?.Invoke();
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			Logger.Debug("Setting up post graphics...");
			SetupPostGraphics();
		}

		WasInitialized = true;
		OnInitializeEvent?.Invoke();

		Instance = instance;
		Instance.Assembly = typeof(T).Assembly;
		Instance.InvokeOnSetupFinished();

		OnStartEvent?.Invoke();

		GameLoop();
		Logger.Info("GameLoop exited");

		// gameloop done
		OnShutdownEvent?.Invoke();
	}

	private void GameLoop() {
		const long TicksPerSecond = 1000000000; // Stopwatch.Frequency;

		long updateTicksToWait = TicksPerSecond / TargetUps;
		long frameTicksToWait = TargetFps == 0 ? 0 : TicksPerSecond / TargetFps;

		long currentTime = Stopwatch.GetTimestamp();
		long updateAccumulator = 0;
		long lastFrameTime = 0;

		Logger.Debug("Entering loop...");

		while (shouldRunGameLoop) {
			TryProcessEvents();

			if (Instance.ShouldShutdown) { shouldRunGameLoop = false; } // check more?
			if (!shouldRunGameLoop) { break; } // Early exit

			// update
			long time = PerformanceMonitor.GetTimeDifference(ref currentTime);
			Update(time);

			// console end. VK/GL graphics below
			switch (GraphicsApi) {
				case GraphicsApi.None: continue;
				case GraphicsApi.Console:
					Render(time); // TODO impl console rendering
					continue;
				case GraphicsApi.OpenGL or GraphicsApi.Vulkan: break;
				default: throw new ArgumentOutOfRangeException();
			}

			// try clean
			TryCleanupBeforeRendering();

			// render
			Render(time);
			RenderCleanup();
		}

		return;

		void Update(long time) {
			updateAccumulator += time;
			PerformanceMonitor.AddUpdateAccumulator(time);

			int frameSkip = 0;
			while (updateAccumulator >= updateTicksToWait && (MaxFrameSkip == 0 || frameSkip < MaxFrameSkip)) {
				PerformanceMonitor.StartTimingUpdate();
				EngineUpdate();
				Instance.Update();
				UpdateCleanup();
				PerformanceMonitor.StopTimingUpdate();

				updateAccumulator -= updateTicksToWait;
				UpdateIndex++;
				frameSkip++;
				PerformanceMonitor.AddUpdate();

				PerformanceMonitor.CheckUpdateTime();

				if (MaxFrameSkip != 0 && frameSkip >= MaxFrameSkip) { Logger.Warn($"FrameSkip hit max. ({MaxFrameSkip})"); }
			}
		}

		void Render(long time) {
			if (TargetFps != 0) {
				while (Stopwatch.GetTimestamp() < lastFrameTime + frameTicksToWait) { Thread.Sleep(0); }
				lastFrameTime = Stopwatch.GetTimestamp();
			}

			PerformanceMonitor.AddFrameAccumulator(time);

			float delta = 1 - (float)(updateTicksToWait - updateAccumulator) / updateTicksToWait;

			PerformanceMonitor.StartTimingFrame();
			foreach (Renderer renderer in renderers.AsValueEnumerable().Where(static renderer => renderer is { ShouldRender: true, })) { renderer.Render(delta); }
			PerformanceMonitor.StopTimingFrame();

			FrameIndex++;
			PerformanceMonitor.AddFrame();

			PerformanceMonitor.CheckFrameTime();
		}
	}

	protected virtual void TryCleanupBeforeRendering() {
		foreach (Renderer renderer in renderers.AsValueEnumerable().Where(static renderer => renderer.ShouldDestroy)) {
			Logger.Debug($"Found {nameof(Renderer)} to destroy...");
			renderersCloseQueue.Enqueue(renderer);
		}

		while (renderersCloseQueue.TryDequeue(out Renderer? renderer)) { RemoveRenderer(renderer); }
	}

	public void AddRenderer<T>(T renderer) where T : Renderer {
		if (GraphicsApi == GraphicsApi.None) {
			Logger.Warn("Cannot add renderers with no graphics backend");
			return;
		}

		Logger.Trace("Renderer added");
		renderers.Add(renderer);
	}

	protected void RemoveRenderer<T>(T renderer) where T : Renderer {
		if (renderers.Remove(renderer)) {
			Logger.Debug($"Destroying {nameof(Renderer)}...");
			renderer.Destroy();
		} else { Logger.Error($"Could not find to be destroyed {nameof(Renderer)} in {nameof(Engine3)}'s {nameof(Renderer)} list"); }
	}

	protected virtual void UpdateCleanup() { }
	protected virtual void RenderCleanup() { }

	protected virtual void PrintSystemInfo() {
		Logger.Debug("System Info");
		Logger.Debug($"OS: {RuntimeInformation.OSDescription}");
	}

	protected virtual void PrintEngineSettings() {
		Logger.Trace("Engine Settings");
		Logger.Debug($"- Engine Version: {Version}");
		Logger.Trace($"- {nameof(GraphicsApi)}: {GraphicsApi}");
		Logger.Trace($"- {nameof(MainThreadName)}: {MainThreadName}");
	}

	protected virtual void SetupPreGraphics() {
		// setup
	}

	protected virtual void SetupPostGraphics() {
		// setup
	}

	protected abstract void SetupConsoleGraphics(EngineGame game);
	protected abstract void SetupOpenGLGraphics(EngineGame game);
	protected abstract void SetupVulkanGraphics(EngineGame game);

	protected abstract void TryProcessEvents();

	protected virtual void EngineUpdate() {
		foreach (Renderer renderer in renderers.AsValueEnumerable().Where(static r => r is { WasDestroyed: false, })) { renderer.Update(); }
	}

	private void Cleanup() {
		Logger.Debug("Cleaning up everything...");
		Logger.Debug("Cleaning up game...");
		Instance.Cleanup();

		Logger.Debug("Cleaning up engine...");
		CleanupEngine();

		Logger.Debug("Cleaning up graphics...");
		CleanupGraphics();

		Instance = null!;
		Engine = null!;

		Logger.Debug("Cleaning OS compatability...");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Windows.Cleanup(); } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Linux.Cleanup(); }

		Logger.Info("Shutting down logger. Goodbye!");
		LogManager.Shutdown();
	}

	protected virtual void CleanupEngine() {
		Logger.Debug($"Cleaning up {renderers.Count} {nameof(Renderer)}s...");
		foreach (Renderer renderer in renderers) { renderer.Destroy(); }
	}

	protected abstract void CleanupGraphics();

	public void Dispose() {
		if (WasDestroyed) { throw new Engine3Exception($"Attempted to dispose {nameof(Engine3)} twice"); }
		if (!WasInitialized) { return; }

		OnShutdownEvent?.Invoke();

		Cleanup();

		WasDestroyed = true;
	}

	public delegate void OnConsoleGraphicsSetupDoneDelegate();
	public delegate void OnOpenGLGraphicsSetupDoneDelegate();
	public delegate void OnVulkanGraphicsSetupDoneDelegate();

	public delegate void OnInitializeDelegate();
	public delegate void OnStartDelegate();
	public delegate void OnShutdownDelegate();
}