using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Console;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Exceptions;
using Engine3.Utility;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Platform;
using Window = Engine3.Client.Window;

namespace Engine3;

public abstract class GameClient {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public string Name { get; }
	public IPackableVersion Version { get; }
	public EngineGraphicsBackend GraphicsBackend { get; }
	public Assembly Assembly { get; }

	private readonly List<Window> windows = new();
	private readonly List<Renderer> renderers = new();

	/// <summary> The amount of updates per second to aim for </summary>
	/// <exception cref="Engine3Exception"> Thrown if value was set to zero </exception>
	public ushort TargetUps { get; init => field = TargetUps != 0 ? value : throw new Engine3Exception($"{nameof(TargetUps)} must be above zero"); } = 60;
	/// <summary> The amount of frames per second to aim for. If zero, framerate will be uncapped </summary>
	public ushort TargetFps { get; init; }
	/// <summary> The maximum amount of frames to skip while updating before rendering anyway. Set to zero to disable </summary>
	public byte MaxFrameSkip { get; init; } = 5;

	public ulong UpdateIndex { get; private set; }
	public ulong FrameIndex { get; private set; }

	public PerformanceMonitor PerformanceMonitor { get; init; } = new();

	public bool WasGraphicsSetup { get; private set; }

	private readonly Queue<Window> windowCloseQueue = new();
	private readonly Queue<Renderer> renderersCloseQueue = new();

	public bool ShouldRunGameLoop { get; private set; } = true;

	private bool requestShutdown;

	public event OnPreGraphicsSetupDelegate? OnPreGraphicsSetupEvent;
	public event OnPostGraphicsSetupDelegate? OnPostGraphicsSetupEvent;
	public event OnSetupFinishedDelegate? OnSetupFinishedEvent;
	public event OnShutdownDelegate? OnShutdownEvent;

	protected GameClient(string name, IPackableVersion version, EngineGraphicsBackend graphicsBackend) {
		Name = name;
		Version = version;
		GraphicsBackend = graphicsBackend;
		Assembly = Assembly.GetCallingAssembly();

		if (GraphicsBackend is OpenGLBackend glBackend) {
			OpenGLGraphicsApiHints graphicsApiHints = glBackend.GraphicsApiHints as OpenGLGraphicsApiHints ?? throw new NullReferenceException();
			graphicsApiHints.Version = new(4, 6);
			graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
			graphicsApiHints.DebugFlag = true;
#endif
		}
	}

	internal void Start() {
		// validate
		if (GraphicsBackend is { GraphicsBackend: not Client.Graphics.GraphicsBackend.Console, GraphicsApiHints: null, }) { throw new Engine3Exception($"GraphicsApiHints cannot be null with GraphicsApi: {GraphicsBackend}"); }

		// print
		Logger.Info("Setting up game...");
		Logger.Debug($"- Game Version: {Version}");

		// setup graphics
		OnPreGraphicsSetupEvent?.Invoke();

		Logger.Debug($"Setting up {Enum.GetName(GraphicsBackend.GraphicsBackend)}...");
		GraphicsBackend.Setup(this);
		WasGraphicsSetup = true;

		OnPostGraphicsSetupEvent?.Invoke();

		// setup done
		Logger.Debug("Setup finished. Invoking events then entering loop");
		OnSetupFinishedEvent?.Invoke();

		GameLoop();
		Logger.Info("GameLoop exited");

		Shutdown();
	}

	protected abstract void Update();
	protected abstract void Cleanup();

	private void EngineUpdate() {
		foreach (Renderer renderer in renderers.Where(static r => r is { WasDestroyed: false, })) { renderer.Update(); }
	}

	private void UpdateCleanup() {
		foreach (Window window in windows) { window.MouseManager.ResetScroll(); }
	}

	private void GameLoop() {
		const long TicksPerSecond = 1000000000; // Stopwatch.Frequency;

		long updateTicksToWait = TicksPerSecond / TargetUps;
		long frameTicksToWait = TargetFps == 0 ? 0 : TicksPerSecond / TargetFps;

		long currentTime = Stopwatch.GetTimestamp();
		long updateAccumulator = 0;
		long lastFrameTime = 0;

		bool isConsole = GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console;

		Logger.Debug("Entering loop...");

		while (ShouldRunGameLoop) {
			if (!isConsole) { Toolkit.Window.ProcessEvents(false); }
			if (requestShutdown) { ShouldRunGameLoop = false; } // check more?

			if (!ShouldRunGameLoop) { break; } // Early exit

			// update
			long time = PerformanceMonitor.GetTimeDifference(ref currentTime);
			Update(time);

			// console end. VK/GL graphics below // TODO impl console rendering
			if (isConsole) {
				Render(time);
				continue;
			}

			// try clean
			TryCloseWindows();
			TryDestroyRenderers();

			// render
			Render(time);

			// reset
			ImGuiH.ResetWidgetOffset();
		}

		return;

		void Update(long time) {
			updateAccumulator += time;
			PerformanceMonitor.AddUpdateAccumulator(time);

			int frameSkip = 0;
			while (updateAccumulator >= updateTicksToWait && (MaxFrameSkip == 0 || frameSkip < MaxFrameSkip)) {
				PerformanceMonitor.StartTimingUpdate();
				EngineUpdate();
				this.Update();
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
				while (Stopwatch.GetTimestamp() < lastFrameTime + frameTicksToWait) { Thread.Sleep(1); }
				lastFrameTime = Stopwatch.GetTimestamp();
			}

			PerformanceMonitor.AddFrameAccumulator(time);

			float delta = 1 - (float)(updateTicksToWait - updateAccumulator) / updateTicksToWait;

			PerformanceMonitor.StartTimingFrame();

			if (!isConsole) {
				foreach (Renderer renderer in renderers.Where(static renderer => renderer is { CanRender: true, IsHidden: false, })) { renderer.Render(delta); }
			} else {
				ConsoleGraphicsBackend backend = (ConsoleGraphicsBackend)GraphicsBackend;
				backend.UpdateBuffer(delta);
				backend.RenderBuffer();
			}

			PerformanceMonitor.StopTimingFrame();

			FrameIndex++;
			PerformanceMonitor.AddFrame();

			PerformanceMonitor.CheckFrameTime();
		}

		void TryCloseWindows() {
			foreach (Window window2 in windows.Where(static window => window.ShouldClose)) {
				Logger.Debug("Found window to destroy...");
				windowCloseQueue.Enqueue(window2);
			}

			while (windowCloseQueue.TryDequeue(out Window? window)) { RemoveWindow(window); }
		}

		void TryDestroyRenderers() {
			foreach (Renderer renderer in renderers.Where(static renderer => renderer.ShouldDestroy)) {
				Logger.Debug($"Found {nameof(Renderer)} to destroy...");
				renderersCloseQueue.Enqueue(renderer);
			}

			while (renderersCloseQueue.TryDequeue(out Renderer? renderer)) { RemoveRenderer(renderer); }
		}

		void RemoveWindow<T>(T window) where T : Window {
			if (windows.Remove(window)) {
				foreach (Renderer renderer in renderers.Where(renderer => renderer.IsSameWindow(window))) { RemoveRenderer(renderer); }

				Logger.Debug($"Destroying {nameof(Window)}...");
				window.Destroy();
			} else { Logger.Error($"Could not find to be destroyed {nameof(Window)} in {nameof(GameClient)}'s {nameof(Window)} list"); }
		}

		void RemoveRenderer<T>(T renderer) where T : Renderer {
			if (renderers.Remove(renderer)) {
				Logger.Debug($"Destroying {nameof(Renderer)}...");
				renderer.Destroy();
			} else { Logger.Error($"Could not find to be destroyed {nameof(Renderer)} in {nameof(GameClient)}'s {nameof(Renderer)} list"); }
		}
	}

	protected void AddWindow<T>(T window) where T : Window {
		if (GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console) {
			Logger.Warn("Cannot add windows when using Console graphics api");
			return;
		}

		Logger.Trace("Window added");
		windows.Add(window);
	}

	protected void AddRenderer<T>(T renderer) where T : Renderer {
		if (GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console) {
			Logger.Warn("Cannot add renderers when using Console graphics api");
			return;
		}

		Logger.Trace("Renderer added");
		renderers.Add(renderer);
	}

	internal bool FindWindow(WindowHandle windowHandle, [NotNullWhen(true)] out Window? window) => (window = windows.Find(w => w.WindowHandle == windowHandle)) != null;

	/// <summary> Requests shutdown. Program will shut down on the next update </summary>
	public void RequestShutdown() {
		Logger.Debug("Requested shutdown");
		requestShutdown = true;
	}

	private void Shutdown() {
		Logger.Debug("Shutdown started");
		OnShutdownEvent?.Invoke();

		Logger.Debug("Cleaning up everything...");
		CleanupEverything();
	}

	private void CleanupEverything() {
		Logger.Debug("Cleaning up instance...");
		Cleanup();

		Logger.Debug($"Cleaning up {renderers.Count} {nameof(Renderer)}s...");
		foreach (Renderer renderer in renderers) { renderer.Destroy(); }

		Logger.Debug($"Cleaning up {windows.Count} {nameof(Window)}s...");
		foreach (Window window in windows) { window.Destroy(); }

		Logger.Debug("Cleaning up ImGui...");
		ImGuiH.Cleanup();

		Logger.Debug("Cleaning up graphics...");
		GraphicsBackend.Cleanup();

		Logger.Debug("Cleaning up done");
	}

	public delegate void OnPreGraphicsSetupDelegate();
	public delegate void OnPostGraphicsSetupDelegate();
	public delegate void OnSetupFinishedDelegate();
	public delegate void OnShutdownDelegate();
}