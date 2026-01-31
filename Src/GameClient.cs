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
	public abstract class GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[field: MaybeNull] public Assembly Assembly { get => field ?? throw new Engine3Exception($"Attempted to get GameInstance Assembly too early. Must call {nameof(GameClient)}#{nameof(Start)} first"); private set; }

		public IPackableVersion Version { get; }
		public string Name { get; }

		public EngineGraphicsBackend GraphicsBackend { get; }
		public Shaderc Shaderc { get; } = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames()));

		protected List<Window> Windows { get; } = new();
		protected List<Renderer> Renderers { get; } = new();

		public ulong UpdateCount { get; private set; }
		public uint UpdatesPerSecond { get; private set; }
		public float UpdateTime { get; private set; }

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

			LoggerH.Setup();
			Logger.Debug("Finished setting up NLog");

			Assembly = Assembly.GetCallingAssembly();
			Logger.Debug("Got instance assembly");

			Engine3.GameInstance = gameClient;

			if (GraphicsBackend is { GraphicsBackend: Client.Graphics.GraphicsBackend.Console, GraphicsApiHints: null, }) { throw new Engine3Exception($"GraphicsApiHints cannot be null with GraphicsApi: {GraphicsBackend}"); }

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine Version: {Engine3.Version}");
			Logger.Debug($"- Game Version: {Version}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW, & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
			Logger.Debug($"- Graphics Api: {GraphicsBackend.GraphicsBackend}");

			uint spvVersion = 0, spvRevision = 0;
			Shaderc.GetSpvVersion(ref spvVersion, ref spvRevision);
			Logger.Debug($"- SpirV Version: {(spvVersion & 16711680U) >> 16}.{(spvVersion & 65280U) >> 8} - {spvRevision}");

			SetupEngine(settings);

			if (GraphicsBackend.GraphicsBackend != Client.Graphics.GraphicsBackend.Console) { SetupGraphics(); }

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
#if DEBUG
			Logger.Debug("Writing dumps to file outputs...");
			StructLayoutDumper.WriteDumpsToOutput();
#endif

			Stbi.SetFlipVerticallyOnLoad(settings.StbiFlipOnLoad);
		}

		private void EngineUpdate() { }

		private void GameLoop() {
			while (shouldRunGameLoop) {
				if (GraphicsBackend.GraphicsBackend != Client.Graphics.GraphicsBackend.Console) { Toolkit.Window.ProcessEvents(false); }
				if (requestShutdown) { shouldRunGameLoop = false; } // TODO check more?

				if (!shouldRunGameLoop) { break; } // Early exit

				EngineUpdate();
				Update();
				UpdateCount++;

				// console end. VK/GL graphics below TODO impl graphics rendering
				if (GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console) { continue; }

				float delta = 0; // TODO impl

				TryCloseWindows();
				TryDestroyRenderers();

				foreach (Renderer pipeline in Renderers.Where(static pipeline => pipeline.CanRender)) { pipeline.Render(delta); }
			}

			return;

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
			public string MainThreadName { get; init; } = "Main";
			public bool StbiFlipOnLoad { get; init; } = true;
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