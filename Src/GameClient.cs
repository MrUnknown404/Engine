using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Debug;
using Engine3.Exceptions;
using Engine3.Utils;
using NLog;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Core.Loader;
using Silk.NET.Shaderc;
using ZLinq;
using GraphicsApi = Engine3.Graphics.GraphicsApi;
using Window = Engine3.Graphics.Window;

namespace Engine3 {
	public abstract partial class GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[field: MaybeNull] public Assembly Assembly { get => field ?? throw new Engine3Exception($"Attempted to get GameInstance Assembly too early. Must call {nameof(GameClient)}#{nameof(Start)} first"); private set; }

		public Version4 Version { get; }
		public string Name { get; }
		public GraphicsApi GraphicsApi { get; }
		public GraphicsApiHints? GraphicsApiHints { get; }
		public List<Window> Windows { get; } = new();
		public Shaderc Shaderc { get; } = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames()));

		public ulong UpdateCount { get; private set; }
		public uint UpdatesPerSecond { get; private set; }
		public float UpdateTime { get; private set; }

		public bool WasGraphicsSetup => WasOpenGLSetup | WasVulkanSetup;

		private bool wasSetup;
		private bool shouldRunGameLoop = true;
		private bool requestShutdown;

		public event Action? OnSetupFinishedEvent;
		public event Action? OnSetupToolkitEvent;
		public event Action? OnShutdownEvent;

		protected GameClient(string name, Version4 version, GraphicsApi graphicsApi) {
			Name = name;
			Version = version;
			GraphicsApi = graphicsApi;
		}

		protected GameClient(string name, Version4 version, OpenGLGraphicsApiHints graphicsApiHints) : this(name, version, GraphicsApi.OpenGL) {
			graphicsApiHints.Version = new(4, 6);
			graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
			graphicsApiHints.DebugFlag = true;
#endif

			GraphicsApiHints = graphicsApiHints;
		}

		protected GameClient(string name, Version4 version, VulkanGraphicsApiHints graphicsApiHints) : this(name, version, GraphicsApi.Vulkan) => GraphicsApiHints = graphicsApiHints;

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
			Logger.Debug($"- Game Version: {Version}");
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

				if (GraphicsApi == GraphicsApi.Console) { continue; }

				float delta = 0; // TODO impl

				foreach (Window window in Windows.Where(static w => w is { WasDestroyed: false, })) {
					if (window.ShouldClose) {
						window.DestroyWindow();
						continue;
					}

					if (window.Renderer is { } renderer) { renderer.InternalDrawFrame(delta); }
				}
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

			Window[] windowsToDestroy = Windows.Where(static w => !w.WasDestroyed).ToArray();

			Logger.Debug($"Cleaning up {Windows.Count} windows...");
			foreach (Window window in windowsToDestroy) { window.DestroyWindow(); }

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