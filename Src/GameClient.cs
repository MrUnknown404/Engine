using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Exceptions;
using Engine3.Graphics;
using Engine3.Utils;
using Engine3.Utils.Versions;
using NLog;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Core.Loader;
using Silk.NET.Shaderc;
using ZLinq;
using GraphicsApi = Engine3.Graphics.GraphicsApi;
using Window = Engine3.Graphics.Window;

#if DEBUG
using Engine3.Debug;
#endif

namespace Engine3 {
	public abstract partial class GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[field: MaybeNull] public Assembly Assembly { get => field ?? throw new Engine3Exception($"Attempted to get GameInstance Assembly too early. Must call {nameof(GameClient)}#{nameof(Start)} first"); private set; }

		public IPackableVersion PackableVersion { get; }
		public string Name { get; }
		public GraphicsApi GraphicsApi { get; }
		public GraphicsApiHints? GraphicsApiHints { get; }
		public Shaderc Shaderc { get; } = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames()));

		public List<Window> Windows { get; } = new();
		protected List<IRenderer> RenderingPipelines { get; } = new();

		public ulong UpdateCount { get; private set; }
		public uint UpdatesPerSecond { get; private set; }
		public float UpdateTime { get; private set; }

		public bool WasGraphicsSetup => WasOpenGLSetup | WasVulkanSetup;

		private readonly Queue<Window> windowCloseQueue = new();
		private readonly Queue<IRenderer> renderingPipelineCloseQueue = new();

		private bool wasSetup;
		private bool shouldRunGameLoop = true;
		private bool requestShutdown;

		public event Action? OnSetupFinishedEvent;
		public event Action? OnSetupToolkitEvent;
		public event Action? OnShutdownEvent;

		protected GameClient(string name, IPackableVersion version, GraphicsApi graphicsApi) {
			Name = name;
			PackableVersion = version;
			GraphicsApi = graphicsApi;
		}

		protected GameClient(string name, IPackableVersion version, OpenGLGraphicsApiHints graphicsApiHints) : this(name, version, GraphicsApi.OpenGL) {
			graphicsApiHints.Version = new(4, 6);
			graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
			graphicsApiHints.DebugFlag = true;
#endif

			GraphicsApiHints = graphicsApiHints;
		}

		protected GameClient(string name, IPackableVersion version, VulkanGraphicsApiHints graphicsApiHints) : this(name, version, GraphicsApi.Vulkan) => GraphicsApiHints = graphicsApiHints;

		public void Start<T>(T gameClient, StartupSettings settings) where T : GameClient {
			if (wasSetup) { throw new Engine3Exception("Attempted to call #Start twice"); }

			Thread.CurrentThread.Name = settings.MainThreadName;

			LoggerH.Setup();
			Logger.Debug("Finished setting up NLog");

			Assembly = Assembly.GetCallingAssembly();
			Logger.Debug("Got instance assembly");

			Engine3.GameInstance = gameClient;
			if (GraphicsApi != GraphicsApi.Console && GraphicsApiHints == null) { throw new Engine3Exception($"GraphicsApiHints cannot be null with GraphicsApi: {GraphicsApi}"); }

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine Version: {Engine3.Version}");
			Logger.Debug($"- Game Version: {PackableVersion}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW, & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
			Logger.Debug($"- Graphics Api: {GraphicsApi}");

			uint spvVersion = 0, spvRevision = 0;
			Shaderc.GetSpvVersion(ref spvVersion, ref spvRevision);
			Logger.Debug($"- SpirV Version: {(spvVersion & 16711680U) >> 16}.{(spvVersion & 65280U) >> 8} - {spvRevision}");

			SetupEngine();

#if DEBUG
			Logger.Debug("Writing dumps to file outputs...");
			StructLayoutDumper.WriteDumpsToOutput();
#endif

			if (GraphicsApi != GraphicsApi.Console) {
				Logger.Info("Setting up Toolkit...");
				SetupToolkit(new() {
						Logger = new TkLogger(),
						FeatureFlags = GraphicsApi switch {
								GraphicsApi.OpenGL => ToolkitFlags.EnableOpenGL,
								GraphicsApi.Vulkan => ToolkitFlags.EnableVulkan,
								GraphicsApi.Console => throw new UnreachableException(),
								_ => throw new ArgumentOutOfRangeException(),
						},
				});

				OnSetupToolkitEvent?.Invoke();
			}

			switch (GraphicsApi) {
				case GraphicsApi.OpenGL:
					Logger.Info("Setting up OpenGL...");
					SetupOpenGL();
					break;
				case GraphicsApi.Vulkan:
					Logger.Info("Setting up Vulkan...");
					SetupVulkan();
					break;
				case GraphicsApi.Console: break;
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

		private void SetupEngine() { }

		private void EngineUpdate() { }

		private void GameLoop() {
			while (shouldRunGameLoop) {
				if (GraphicsApi != GraphicsApi.Console) { Toolkit.Window.ProcessEvents(false); }
				if (requestShutdown) { shouldRunGameLoop = false; } // TODO check more?
				if (!shouldRunGameLoop) { break; } // Early exit

				EngineUpdate();
				Update();
				UpdateCount++;

				// console end. VK/GL graphics below TODO impl graphics rendering
				if (GraphicsApi == GraphicsApi.Console) { continue; }

				float delta = 0; // TODO impl

				EnqueueToCloseWindows();
				DestroyEnqueuedWindows();

				EnqueueInvalidRenderingPipelines();
				DestroyEnqueuedRenderingPipelines();

				foreach (IRenderer pipeline in RenderingPipelines.Where(static pipeline => pipeline.CanRender)) { pipeline.Render(delta); }
			}

			return;

			void EnqueueToCloseWindows() {
				foreach (Window window in Windows.Where(static window => window.ShouldClose)) {
					Logger.Debug("Found window to destroy...");
					windowCloseQueue.Enqueue(window);
				}
			}

			void DestroyEnqueuedWindows() {
				while (windowCloseQueue.TryDequeue(out Window? window)) {
					if (Windows.Remove(window)) {
						Window result = window;
						foreach (IRenderer pipeline in RenderingPipelines.Where(pipeline => pipeline.IsSameWindow(result))) { DestroyRenderingPipeline(pipeline); }

						Logger.Debug("Destroying window...");
						window.Destroy();
					} else { Logger.Error("Could not find to be destroyed window in game client window list"); }
				}
			}

			void EnqueueInvalidRenderingPipelines() {
				foreach (IRenderer pipeline in RenderingPipelines.Where(static pipeline => pipeline.ShouldDestroy)) {
					Logger.Debug("Found rendering pipeline to destroy...");
					renderingPipelineCloseQueue.Enqueue(pipeline);
				}
			}

			void DestroyEnqueuedRenderingPipelines() {
				while (renderingPipelineCloseQueue.TryDequeue(out IRenderer? pipeline)) { DestroyRenderingPipeline(pipeline); }
			}

			void DestroyRenderingPipeline(IRenderer pipeline) {
				if (RenderingPipelines.Remove(pipeline)) {
					Logger.Debug("Destroying rendering pipeline...");
					pipeline.Destroy();
				} else { Logger.Error("Could not find to be destroyed rendering pipeline in game client rendering pipeline list"); }
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

			Logger.Debug("Cleaning up instance...");
			Cleanup();

			Logger.Debug($"Cleaning up {RenderingPipelines.Count} rendering pipelines...");
			foreach (IRenderer pipeline in RenderingPipelines) { pipeline.Destroy(); }

			Logger.Debug($"Cleaning up {Windows.Count} windows...");
			foreach (Window window in Windows) { window.Destroy(); }

			Logger.Debug("Cleaning up graphics api...");
			switch (GraphicsApi) {
				case GraphicsApi.Console: break;
				case GraphicsApi.OpenGL: CleanupOpenGL(); break;
				case GraphicsApi.Vulkan: CleanupVulkan(); break;
				default: throw new ArgumentOutOfRangeException();
			}

			Logger.Info("Goodbye!");
			LogManager.Shutdown();
		}

		public class StartupSettings {
			public string MainThreadName { get; init; } = "Main";
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