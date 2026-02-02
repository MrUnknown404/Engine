using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Client.Graphics;
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

namespace Engine3 {
	// TODO call timeBeginPeriod/timeEndPeriod on windows https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-sleep

	public abstract class GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[field: MaybeNull] public Assembly Assembly { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(GameClient)} Assembly too early. Must call {nameof(GameClient)}#{nameof(Start)} first"); private set; }

		public IPackableVersion Version { get; }
		public string Name { get; }

		public EngineGraphicsBackend GraphicsBackend { get; }
		public Shaderc Shaderc { get; } = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames()));

		protected List<Window> Windows { get; } = new();
		protected List<Renderer> Renderers { get; } = new();

		public ushort UpsTarget { get; init; } = 60;
		public ushort FpsTarget { get; init; }
		public byte MaxFrameSkip { get; init; } = 5;

		public ulong UpdateIndex { get; private set; }
		public ulong FrameIndex { get; private set; }

		public uint Ups { get; private set; }
		public uint Fps { get; private set; }

		/// <summary> In milliseconds </summary>
		public float UpdateTime { get; private set; }
		/// <summary> In milliseconds </summary>
		public float FrameTime { get; private set; }

		public bool WasGraphicsSetup { get; private set; }

		private readonly Queue<Window> windowCloseQueue = new();
		private readonly Queue<Renderer> renderersCloseQueue = new();

		private bool wasSetup;
		private bool shouldRunGameLoop = true;
		private bool requestShutdown;

		public event Action? OnSetupFinishedEvent;
		public event Action? OnSetupToolkitEvent;
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

		public void Start<T>(T gameClient, StartupSettings settings) where T : GameClient {
			if (wasSetup) { throw new Engine3Exception("Attempted to call #Start twice"); }

			Thread.CurrentThread.Name = settings.MainThreadName;

			LoggerH.Setup(GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console);
			Logger.Debug("Finished setting up NLog");

			Assembly = Assembly.GetCallingAssembly();
			Logger.Debug("Got instance assembly");

			Engine3.GameInstance = gameClient;

			if (GraphicsBackend is { GraphicsBackend: not Client.Graphics.GraphicsBackend.Console, GraphicsApiHints: null, }) { throw new Engine3Exception($"GraphicsApiHints cannot be null with GraphicsApi: {GraphicsBackend}"); }

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine Version: {Engine3.Version}");
			Logger.Debug($"- Game Version: {Version}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW, & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
			Logger.Debug($"- Graphics Api: {GraphicsBackend.GraphicsBackend}");

			uint spvVersion = 0, spvRevision = 0;
			Shaderc.GetSpvVersion(ref spvVersion, ref spvRevision);
			Logger.Debug($"- SpirV Version: {(spvVersion & 16711680) >> 16}.{(spvVersion & 65280) >> 8} - {spvRevision}");

			SetupEngine(settings);

			switch (GraphicsBackend.GraphicsBackend) {
				case Client.Graphics.GraphicsBackend.Console: SetupConsole(); break;
				case Client.Graphics.GraphicsBackend.OpenGL:
				case Client.Graphics.GraphicsBackend.Vulkan: SetupGraphics(); break;
				default: throw new ArgumentOutOfRangeException();
			}

			wasSetup = true;

			Logger.Debug("Setup finished. Invoking events then entering loop");
			OnSetupFinishedEvent?.Invoke();

			GameLoop();

			Logger.Info("GameLoop exited");
			OnShutdownEvent?.Invoke();

			CleanupEverything();
			Environment.Exit(0);
		}

		protected abstract void Update();
		protected abstract void Cleanup();

		private void SetupConsole() {
			Logger.Info("Setting up Console...");
			throw new NotImplementedException();
		}

		private void SetupGraphics() {
			Logger.Info("Setting up Toolkit...");
			SetupToolkit(new() {
					Logger = new TkLogger(),
					FeatureFlags = GraphicsBackend.GraphicsBackend switch {
							Client.Graphics.GraphicsBackend.OpenGL => ToolkitFlags.EnableOpenGL,
							Client.Graphics.GraphicsBackend.Vulkan => ToolkitFlags.EnableVulkan,
							Client.Graphics.GraphicsBackend.Console => throw new UnreachableException(),
							_ => throw new ArgumentOutOfRangeException(),
					},
			});

			OnSetupToolkitEvent?.Invoke();

			Logger.Debug($"Setting up {Enum.GetName(GraphicsBackend.GraphicsBackend)}...");
			GraphicsBackend.Setup(this);
			WasGraphicsSetup = true;
		}

		private void SetupEngine(StartupSettings settings) {
			settings.Print();

#if DEBUG
			Logger.Debug("Writing dumps to file outputs...");
			StructLayoutDumper.WriteDumpsToOutput();
#endif

			Stbi.SetFlipVerticallyOnLoad(settings.StbiFlipOnLoad);
		}

		private void EngineUpdate() { }

		private void GameLoop() {
			const long TicksPerSecond = 1000000000; // Stopwatch.Frequency;
			const long TicksPerMillisecond = TicksPerSecond / 1000;

			long updateTicksToWait = TicksPerSecond / UpsTarget;
			long frameTicksToWait = FpsTarget == 0 ? 0 : TicksPerSecond / FpsTarget;

			long currentTime = Stopwatch.GetTimestamp();
			long updateAccumulator = 0;
			long updateCounterAccumulator = 0;
			long frameCounterAccumulator = 0;
			long lastFrameTime = 0;

			uint updateCounter = 0;
			uint frameCounter = 0;

			while (shouldRunGameLoop) { // TODO optional fps cap
				if (GraphicsBackend.GraphicsBackend != Client.Graphics.GraphicsBackend.Console) { Toolkit.Window.ProcessEvents(false); }
				if (requestShutdown) { shouldRunGameLoop = false; } // TODO check more?

				if (!shouldRunGameLoop) { break; } // Early exit

				long time = GetTimeDifference();
				Update(time);

				// console end. VK/GL graphics below // TODO impl console rendering
				if (GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console) { continue; }

				TryCloseWindows();
				TryDestroyRenderers();

				Render(time);
			}

			return;

			long GetTimeDifference() {
				long cycleStart = Stopwatch.GetTimestamp();
				long time = cycleStart - currentTime;
				currentTime = cycleStart;
				return time;
			}

			void Update(long time) {
				updateAccumulator += time;
				updateCounterAccumulator += time;

				int frameSkip = 0;
				while (updateAccumulator >= updateTicksToWait && frameSkip < MaxFrameSkip) {
					long updateStart = Stopwatch.GetTimestamp();
					EngineUpdate();
					this.Update();
					long updateEnd = Stopwatch.GetTimestamp();

					UpdateTime = (float)(updateEnd - updateStart) / TicksPerMillisecond;

					updateAccumulator -= updateTicksToWait;
					UpdateIndex++;
					updateCounter++;
					frameSkip++;

					if (updateCounterAccumulator >= TicksPerSecond) {
						Ups = updateCounter;
						updateCounter = 0;
						updateCounterAccumulator -= TicksPerSecond;
					}

					if (frameSkip >= MaxFrameSkip) { Logger.Warn($"FrameSkip hit max. ({MaxFrameSkip})"); }
				}
			}

			void Render(long time) {
				if (FpsTarget != 0) {
					while (Stopwatch.GetTimestamp() < lastFrameTime + frameTicksToWait) { Thread.Sleep(0); }
					lastFrameTime = Stopwatch.GetTimestamp();
				}

				frameCounterAccumulator += time;

				long frameStart = Stopwatch.GetTimestamp();

				float delta = 1 - (float)(updateTicksToWait - updateAccumulator) / updateTicksToWait;
				foreach (Renderer pipeline in Renderers.Where(static pipeline => pipeline.CanRender)) { pipeline.Render(delta); }

				long frameEnd = Stopwatch.GetTimestamp();

				FrameTime = (float)(frameEnd - frameStart) / TicksPerMillisecond;

				FrameIndex++;
				frameCounter++;

				if (frameCounterAccumulator >= TicksPerSecond) {
					Fps = frameCounter;
					frameCounter = 0;
					frameCounterAccumulator -= TicksPerSecond;
				}
			}

			void TryCloseWindows() {
				foreach (Window window2 in Windows.Where(static window => window.ShouldClose)) {
					Logger.Debug("Found window to destroy...");
					windowCloseQueue.Enqueue(window2);
				}

				while (windowCloseQueue.TryDequeue(out Window? window)) {
					if (Windows.Remove(window)) {
						Window tempWindow = window; // a
						foreach (Renderer renderer in Renderers.Where(pipeline => pipeline.IsSameWindow(tempWindow))) { DestroyRenderingPipeline(renderer); }

						Logger.Debug($"Destroying {nameof(Window)}...");
						window.Destroy();
					} else { Logger.Error($"Could not find to be destroyed {nameof(Window)} in {nameof(GameClient)}'s {nameof(Window)} list"); }
				}
			}

			void TryDestroyRenderers() {
				foreach (Renderer pipeline in Renderers.Where(static pipeline => pipeline.ShouldDestroy)) {
					Logger.Debug($"Found {nameof(Renderer)} to destroy...");
					renderersCloseQueue.Enqueue(pipeline);
				}

				while (renderersCloseQueue.TryDequeue(out Renderer? pipeline)) { DestroyRenderingPipeline(pipeline); }
			}

			void DestroyRenderingPipeline(Renderer renderer) {
				if (Renderers.Remove(renderer)) {
					Logger.Debug($"Destroying {nameof(Renderer)}...");
					renderer.Destroy();
				} else { Logger.Error($"Could not find to be destroyed {nameof(Renderer)} in {nameof(GameClient)}'s {nameof(Renderer)} list"); }
			}
		}

		private void SetupToolkit(ToolkitOptions toolkitOptions) {
			EventQueue.EventRaised += (_, _, args) => {
				switch (args) {
					case CloseEventArgs closeArgs: {
						if (Windows.Find(w => w.WindowHandle == closeArgs.Window) is { } window) {
							window.TryCloseWindow();
							return;
						}

						Logger.Warn("Attempted to close an unknown window");
						break;
					}
					case WindowResizeEventArgs resizeArgs: {
						if (Windows.Find(w => w.WindowHandle == resizeArgs.Window) is { } window) {
							window.WasResized = true;
							return;
						}

						Logger.Warn("Attempted to resize an unknown window");
						break;
					}
					case KeyDownEventArgs downArgs: {
						if (Windows.Find(w => w.WindowHandle == downArgs.Window) is { } window) {
							window.InputManager.SetKey(downArgs.Key, true);
							return;
						}

						Logger.Warn("Attempted to provide input to an unknown window");
						break;
					}
					case KeyUpEventArgs upArgs: {
						if (Windows.Find(w => w.WindowHandle == upArgs.Window) is { } window) {
							window.InputManager.SetKey(upArgs.Key, false);
							return;
						}

						Logger.Warn("Attempted to provide input to an unknown window");
						break;
					}
				}
			};

			Toolkit.Init(toolkitOptions);
		}

		public void Shutdown() {
			Logger.Debug("Shutdown called");
			requestShutdown = true;
		}

		private void CleanupEverything() {
			Logger.Debug("Cleaning up everything...");

			Logger.Debug("Cleaning up engine...");
			CleanupEngine();

			Logger.Debug("Cleaning up instance...");
			Cleanup();

			Logger.Debug($"Cleaning up {Renderers.Count} {nameof(Renderer)}s...");
			foreach (Renderer pipeline in Renderers) { pipeline.Destroy(); }

			Logger.Debug($"Cleaning up {Windows.Count} {nameof(Window)}s...");
			foreach (Window window in Windows) { window.Destroy(); }

			Logger.Debug("Cleaning up graphics...");
			GraphicsBackend.Cleanup();

			Logger.Info("Goodbye!");
			LogManager.Shutdown();
		}

		private void CleanupEngine() => Shaderc.Dispose();

		public class StartupSettings {
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

		private class ShadercSearchPathContainer : SearchPathContainer { // TODO rename
			public override string[] Linux => new[] { "libshaderc_shared.so", "libshaderc.so", };
			public override string[] MacOS => new[] { "libshaderc_shared.dylib", };
			public override string[] Android => new[] { "libshaderc_shared.so", };
			public override string[] IOS => new[] { string.Empty, };
			public override string[] Windows64 => new[] { "shaderc_shared.dll", };
			public override string[] Windows86 => new[] { "shaderc_shared.dll", };
		}
	}
}