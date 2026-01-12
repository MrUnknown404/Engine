using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Debug;
using Engine3.Exceptions;
using Engine3.Utils;
using NLog;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using shaderc;
using GraphicsApi = Engine3.Graphics.GraphicsApi;
using Window = Engine3.Graphics.Window;
using SpirVCompiler = shaderc.Compiler;

namespace Engine3 {
	public static partial class Engine3 {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const string Name = "Engine3";

		public const bool Debug =
#if DEBUG
				true;
#else
				false;
#endif

		// ReSharper disable once InconsistentNaming
		private static readonly Lazy<Assembly> engineAssembly = new(static () => Assembly.GetAssembly(typeof(Engine3)) ?? throw new NullReferenceException("Failed to get Engine3 Assembly"));

		public static readonly Version4 EngineVersion = new(0, 0, 0);
		public static Assembly EngineAssembly => engineAssembly.Value;
		public static GraphicsApi GraphicsApi { get; private set; } = GraphicsApi.Console;
		public static GraphicsApiHints? GraphicsApiHints { get; private set; }
		public static GameClient? GameInstance { get; private set; }
		internal static readonly List<Window> Windows = new();

		public static ulong UpdateFrameCount { get; private set; }
		public static ulong RenderFrameCount { get; private set; }
		public static uint Fps { get; private set; }
		public static uint Ups { get; private set; }
		public static float UpdateTime { get; private set; }
		public static float RenderTime { get; private set; }

		private static bool shouldRunGameLoop = true;

		public static event Action? PreEngineSetupEvent;
		public static event Action? PostEngineSetupEvent;
		public static event Action? OnSetupFinishedEvent;
		public static event Action? OnSetupToolkitEvent;
		public static event Action? OnShutdownEvent;

		public static void Start<T>(StartupSettings settings) where T : GameClient, new() {
			if (GameInstance != null) {
				Logger.Error("Attempted to call #Start twice");
				return;
			}

			Thread.CurrentThread.Name = settings.MainTheadName;

			LoggerH.Setup();
			Logger.Debug("Finished setting up NLog");

			GraphicsApi = settings.GraphicsApi;
			GraphicsApiHints = settings.GraphicsApiHints;

			CheckValidStartupSettings(settings);

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine Version: {EngineVersion}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW, & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
			Logger.Debug($"- Graphics Api: {GraphicsApi}");
			SpirVCompiler.GetSpvVersion(out SpirVVersion version, out uint revision);
			Logger.Debug($"- SpirV Version: {version.Major}.{version.Minor} - {revision}");

			PreEngineSetupEvent?.Invoke();
			SetupEngine();
			PostEngineSetupEvent?.Invoke();

			Logger.Debug("Creating game instance...");
			GameInstance = new T { Assembly = Assembly.GetAssembly(typeof(T)) ?? throw new NullReferenceException($"Failed to get assembly for '{typeof(T).Name}'"), };
			Logger.Debug($"- Game Version: {GameInstance.Version}");

			GameInstance.Setup();

#if DEBUG
			Logger.Debug("Writing dumps to file outputs...");
			StructLayoutDumper.WriteDumpsToOutput();
#endif

			if (GraphicsApi != GraphicsApi.Console) {
				Logger.Info("Setting up Toolkit...");
				SetupToolkit(settings.ToolkitOptions!);
				OnSetupToolkitEvent?.Invoke();
			}

			if (GraphicsApi == GraphicsApi.Vulkan) {
				Logger.Info("Setting up Vulkan...");
				SetupVulkan(settings.GameName, GameInstance.Version, EngineVersion);
			}

			Logger.Debug("Setup finished. Invoking events then entering loop");
			OnSetupFinishedEvent?.Invoke();

			GameLoop();

			Logger.Debug("GameLoop exited naturally");
			Shutdown(0);

			return;

			static void CheckValidStartupSettings(StartupSettings settings) {
				switch (GraphicsApi) {
					case GraphicsApi.OpenGL:
						if (settings.ToolkitOptions == null) {
							throw new Engine3Exception("Toolkit cannot be null when using 'OpenGL' GraphicsApi"); //
						} else if (settings.GraphicsApiHints is not OpenGLGraphicsApiHints openglGraphics) {
							throw new Engine3Exception("GraphicsApi was set to 'OpenGL' but the provided GraphicsApiHints was either not of type OpenGLGraphicsApiHints, or null");
						} else if (openglGraphics is not { Version: { Major: 4, Minor: 6, }, Profile: OpenGLProfile.Core, }) {
							throw new Engine3Exception("Engine only supports OpenGL version 4.6 Core"); //
						}

						break;
					case GraphicsApi.Vulkan:
						if (settings.ToolkitOptions == null) {
							throw new Engine3Exception("Toolkit cannot be null when using 'Vulkan' GraphicsApi"); //
						} else if (settings.GraphicsApiHints is not VulkanGraphicsApiHints) {
							throw new Engine3Exception("GraphicsApi was set to 'Vulkan' but the provided GraphicsApiHints was either not of type VulkanGraphicsApiHints, or null"); //
						}

						break;
					case GraphicsApi.Console:
					default: break;
				}
			}
		}

		public static void Shutdown(int errorCode) {
			Logger.Info("Shutdown called");
			OnShutdownEvent?.Invoke();
			shouldRunGameLoop = false;
			Cleanup();
			Environment.Exit(errorCode);
		}

		private static void SetupEngine() { }

		private static void SetupToolkit(ToolkitOptions toolkitOptions) {
			EventQueue.EventRaised += OnTkEventRaised;

			Toolkit.Init(toolkitOptions);

			return;

			static void OnTkEventRaised(PalHandle? handle, PlatformEventType type, EventArgs args) {
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
			}
		}

		private static void GameLoop() {
			Stopwatch stopwatch = new(); // TODO remove
			stopwatch.Start();
			uint fpsCounter = 0;

			while (shouldRunGameLoop) {
				Toolkit.Window.ProcessEvents(false);
				if (!shouldRunGameLoop) { break; } // Early exit

				Update();
				GameInstance!.Update(); // shouldn't be null at this point
				UpdateFrameCount++;

				// RenderContext.PrepareFrame();

				float delta = 0; // TODO impl
				Render(delta);
				GameInstance.Render(delta);

				// RenderContext.FinalizeFrame();
				// RenderContext.SwapBuffers();

				RenderFrameCount++;

				fpsCounter++;
				if (stopwatch.Elapsed.TotalSeconds >= 1) {
					Logger.Debug($"Fps: {fpsCounter}");
					stopwatch.Restart();
					fpsCounter = 0;
				}
			}
		}

		private static void Update() { }

		private static void Render(float delta) { }

		private static void Cleanup() {
			LogManager.Shutdown();

			GameInstance?.Cleanup();

			foreach (Window window in Windows) { window.CloseWindow(false); }

			if (GraphicsApi == GraphicsApi.Vulkan) { CleanupVulkan(); }
		}

		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		public class StartupSettings { // TODO split into OpenGL/Vulkan settings? dunno
			public required string GameName { get; init; }
			public required string MainTheadName { get; init; }
			public GraphicsApi GraphicsApi { get; } = GraphicsApi.Console;
			public ToolkitOptions? ToolkitOptions { get; }
			public GraphicsApiHints? GraphicsApiHints { get; }

			public StartupSettings() { }

			[SetsRequiredMembers]
			public StartupSettings(string gameName, string mainTheadName) {
				GameName = gameName;
				MainTheadName = mainTheadName;
			}

			[SetsRequiredMembers]
			private StartupSettings(string gameName, string mainTheadName, ToolkitOptions toolkitOptions) : this(gameName, mainTheadName) {
				ToolkitOptions = toolkitOptions;
				ToolkitOptions.ApplicationName = gameName;

				toolkitOptions.Logger = new TkLogger();
			}

			[SetsRequiredMembers]
			public StartupSettings(string gameName, string mainTheadName, ToolkitOptions toolkitOptions, OpenGLGraphicsApiHints graphicsApiHints) : this(gameName, mainTheadName, toolkitOptions) {
				GraphicsApi = GraphicsApi.OpenGL;
				GraphicsApiHints = graphicsApiHints;

				toolkitOptions.FeatureFlags = ToolkitFlags.EnableOpenGL;

				graphicsApiHints.Version = new(4, 6);
				graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
				graphicsApiHints.DebugFlag = true;
#endif
			}

			[SetsRequiredMembers]
			public StartupSettings(string title, string mainTheadName, ToolkitOptions toolkitOptions, VulkanGraphicsApiHints graphicsApiHints) : this(title, mainTheadName, toolkitOptions) {
				GraphicsApi = GraphicsApi.Vulkan;
				GraphicsApiHints = graphicsApiHints;

				toolkitOptions.FeatureFlags = ToolkitFlags.EnableVulkan;
			}
		}
	}
}