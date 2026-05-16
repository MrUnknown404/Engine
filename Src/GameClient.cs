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
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Core.Loader;
using Silk.NET.Shaderc;
using StbiSharp;
using Window = Engine3.Client.Window;

#if DEBUG
using Engine3.Debug;
#endif

namespace Engine3;
// TODO call timeBeginPeriod/timeEndPeriod on windows https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-sleep

public abstract class GameClient {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	[field: MaybeNull] public Assembly Assembly { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(GameClient)} Assembly too early. Must call {nameof(GameClient)}#{nameof(Start)} first"); private set; }

	internal Shaderc Shaderc { get; } = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames()));

	public string Name { get; }
	public IPackableVersion Version { get; }
	public EngineGraphicsBackend GraphicsBackend { get; }

	private readonly List<Window> windows = new();
	private readonly List<Renderer> renderers = new();

	/// <summary> The amount of updates per second to aim for </summary>
	/// <exception cref="Engine3Exception"> Thrown if value was set to zero </exception>
	public ushort TargetUps { get; init => field = TargetUps != 0 ? value : throw new Engine3Exception($"{nameof(TargetUps)} must be above zero"); } = 60;
	/// <summary> The amount of frames per second to aim for. If zero, framerate will be uncapped </summary>
	public ushort TargetFps { get; init; }
	/// <summary> The maximum amount of frames to skip while updating before rendering anyways. Set to zero to disable </summary>
	public byte MaxFrameSkip { get; init; } = 5;

	public ulong UpdateIndex { get; private set; }
	public ulong FrameIndex { get; private set; }

	public PerformanceMonitor PerformanceMonitor { get; init; } = new();

	public bool WasGraphicsSetup { get; private set; }

	private readonly Queue<Window> windowCloseQueue = new();
	private readonly Queue<Renderer> renderersCloseQueue = new();

	public bool ShouldRunGameLoop { get; private set; } = true;

	private bool wasSetup;
	private bool requestShutdown;

	/// <summary> Called after <see cref="SetupEngine"/> is done and ready to enter the gameloop </summary>
	public event Action? OnSetupFinishedEvent;
	/// <summary> Called after OpenTK <see cref="Toolkit"/> was set up </summary>
	public event Action? OnSetupToolkitEvent;
	/// <summary> Called at the start of <see cref="Shutdown"/> & before <see cref="CleanupEverything"/> </summary>
	public event Action? OnShutdownEvent;

	protected GameClient(string name, IPackableVersion version, EngineGraphicsBackend graphicsBackend) {
		Name = name;
		Version = version;
		GraphicsBackend = graphicsBackend;

		if (GraphicsBackend is OpenGLGraphicsBackend glBackend) {
			OpenGLGraphicsApiHints graphicsApiHints = glBackend.GraphicsApiHints as OpenGLGraphicsApiHints ?? throw new NullReferenceException();
			graphicsApiHints.Version = new(4, 6);
			graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
			graphicsApiHints.DebugFlag = true;
#endif
		}
	}

	/// <summary> Call to start your game. Some things may need to be set before this is run </summary>
	/// <param name="settings"> Engine startup settings </param>
	/// <exception cref="Engine3Exception"> Thrown if an error occurs </exception>
	public void Start(StartupSettings settings) {
		if (wasSetup) { throw new Engine3Exception("Attempted to call #Start twice"); }

		Thread.CurrentThread.Name = settings.MainThreadName;

		LoggerH.Setup(GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console);
		Logger.Debug("Finished setting up NLog");

		Assembly = Assembly.GetCallingAssembly();
		Logger.Debug("Got instance assembly");

		Engine3.GameInstance = this;

		if (GraphicsBackend is { GraphicsBackend: not Client.Graphics.GraphicsBackend.Console, GraphicsApiHints: null, }) { throw new Engine3Exception($"GraphicsApiHints cannot be null with GraphicsApi: {GraphicsBackend}"); }

		SetupEngine(settings);

		SetupDone();

		GameLoop();
		Logger.Info("GameLoop exited");

		Shutdown();
	}

	protected abstract void Update();
	protected abstract void Cleanup();

	private void SetupEngine(StartupSettings settings) {
		Logger.Info("Setting up engine...");
		Logger.Debug($"- Engine Version: {Engine3.Version}");
		Logger.Debug($"- Game Version: {Version}");
		Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
		Logger.Debug($"- ImGui Version: {ImGuiNet.GetVersion()}");
		Logger.Debug($"- Graphics Api: {GraphicsBackend.GraphicsBackend}");

		uint spvVersion = 0, spvRevision = 0;
		Shaderc.GetSpvVersion(ref spvVersion, ref spvRevision);
		Logger.Debug($"- SpirV Version: {(spvVersion & 16711680) >> 16}.{(spvVersion & 65280) >> 8} - {spvRevision}");

		settings.Print();

#if DEBUG
		Logger.Debug("Writing dumps to file outputs...");
		StructLayoutDumper.WriteDumpsToOutput();
#endif

		Stbi.SetFlipVerticallyOnLoad(settings.StbiFlipOnLoad);

		Logger.Info("Setting up Toolkit...");

		if (GraphicsBackend.GraphicsBackend != Client.Graphics.GraphicsBackend.Console) {
			SetupToolkit(new() {
					ApplicationName = Name,
					Logger = new TkLogger(),
					FeatureFlags = GraphicsBackend.GraphicsBackend switch {
							Client.Graphics.GraphicsBackend.OpenGL => ToolkitFlags.EnableOpenGL,
							Client.Graphics.GraphicsBackend.Vulkan => ToolkitFlags.EnableVulkan,
							Client.Graphics.GraphicsBackend.Console => ToolkitFlags.None,
							_ => throw new ArgumentOutOfRangeException(),
					},
			});

			OnSetupToolkitEvent?.Invoke();
		}

		Logger.Debug($"Setting up {Enum.GetName(GraphicsBackend.GraphicsBackend)}...");
		GraphicsBackend.Setup(this);
		WasGraphicsSetup = true;
	}

	private void SetupDone() {
		wasSetup = true;

		Logger.Debug("Setup finished. Invoking events then entering loop");
		OnSetupFinishedEvent?.Invoke();
	}

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
				while (Stopwatch.GetTimestamp() < lastFrameTime + frameTicksToWait) { Thread.Sleep(0); }
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

	private void SetupToolkit(ToolkitOptions toolkitOptions) {
		EventQueue.EventRaised += OnEventQueueOnEventRaised;

		Toolkit.Init(toolkitOptions);

		return;

		void OnEventQueueOnEventRaised(PalHandle? palHandle, PlatformEventType platformEventType, EventArgs args) {
			if (args is WindowEventArgs windowArgs) {
				if (!FindWindow(windowArgs.Window, out Window? window)) {
					Logger.Warn("EventQueue received on an unknown window");
					return;
				}

				switch (args) {
					case CloseEventArgs: window.OnCloseEventArgs(); break;
					case WindowResizeEventArgs resizeArgs: window.OnResizeEventArgs(resizeArgs); break;
					case WindowModeChangeEventArgs modeArgs: window.OnModeChangeEventArgs(modeArgs); break;
					case KeyDownEventArgs downArgs: window.OnKeyDownEventArgs(downArgs); break;
					case KeyUpEventArgs upArgs: window.OnKeyUpEventArgs(upArgs); break;
					case MouseMoveEventArgs moveArgs: window.OnMouseMoveEventArgs(moveArgs); break;
					case MouseButtonDownEventArgs downArgs: window.OnMouseButtonDownEventArgs(downArgs); break;
					case MouseButtonUpEventArgs upArgs: window.OnMouseButtonUpEventArgs(upArgs); break;
					case ScrollEventArgs scrollArgs: window.OnScrollEventArgs(scrollArgs); break;
				}
			} else {
				// ignored atm
			}
		}

		bool FindWindow(WindowHandle windowHandle, [NotNullWhen(true)] out Window? window) => (window = windows.Find(w => w.WindowHandle == windowHandle)) != null;
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

		Logger.Info("Shutting down logger. Goodbye!");
		LogManager.Shutdown();

		Environment.Exit(0);
	}

	private void CleanupEverything() {
		Logger.Debug("Cleaning up engine...");
		CleanupEngine();

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

	private void CleanupEngine() => Shaderc.Dispose();

	public sealed class StartupSettings {
		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public string MainThreadName { get; init; } = "Main";
		public bool StbiFlipOnLoad { get; init; } = true;

		internal void Print() {
			Logger.Trace("Engine Startup Settings");
			Logger.Trace($"- {nameof(MainThreadName)}: {MainThreadName}");
			Logger.Trace($"- {nameof(StbiFlipOnLoad)}: {StbiFlipOnLoad}");
		}
	}

	private class ShadercSearchPathContainer : SearchPathContainer {
		public override string[] Linux => new[] { "libshaderc_shared.so", "libshaderc.so", };
		public override string[] MacOS => new[] { "libshaderc_shared.dylib", };
		public override string[] Android => new[] { "libshaderc_shared.so", };
		public override string[] IOS => new[] { string.Empty, };
		public override string[] Windows64 => new[] { "shaderc_shared.dll", };
		public override string[] Windows86 => new[] { "shaderc_shared.dll", };
	}
}