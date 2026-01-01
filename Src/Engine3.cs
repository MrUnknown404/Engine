using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Debug;
using Engine3.Exceptions;
using Engine3.Utils;
using NLog;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GraphicsApi = Engine3.Graphics.GraphicsApi;

namespace Engine3 {
	public static partial class Engine3 {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const string Name = "Engine3";

		// ReSharper disable once InconsistentNaming
		private static readonly Lazy<Assembly> engineAssembly = new(static () => Assembly.GetAssembly(typeof(Engine3)) ?? throw new NullReferenceException("Failed to get Engine3 Assembly"));

		public static readonly Version4 EngineVersion = new(0, 0, 0);
		public static Assembly EngineAssembly => engineAssembly.Value;
		public static GraphicsApi GraphicsApi { get; private set; } = GraphicsApi.Console;
		public static GraphicsApiHints? GraphicsApiHints { get; private set; }
		public static GameClient? GameInstance { get; private set; }
		internal static readonly List<EngineWindow> Windows = new();

		public static ulong UpdateFrame { get; private set; }
		public static ulong RenderFrame { get; private set; }
		public static uint Fps { get; private set; }
		public static uint Ups { get; private set; }
		public static float UpdateTime { get; private set; }
		public static float RenderTime { get; private set; }

		public static bool WasGraphicsApiSetup => WasOpenGLSetup | WasVulkanSetup;

		private static bool shouldRunGameLoop = true;

		public static event Action? PreEngineSetupEvent;
		public static event Action? PostEngineSetupEvent;
		public static event Action? OnSetupFinishedEvent;
		public static event Action? OnSetupToolkitEvent;
		public static event Action? OnShutdownEvent;

		public static unsafe void Start<T>(StartupSettings settings) where T : GameClient, new() {
			if (GameInstance != null) {
				Logger.Error("Attempted to call #Start twice");
				return;
			}

			Thread.CurrentThread.Name = settings.MainTheadName;

			LoggerH.Setup();
			Logger.Debug("Finished setting up NLog");

			GraphicsApi = settings.GraphicsApi;
			GraphicsApiHints = settings.GraphicsApiHints;
			Logger.Debug($"Graphics Api is set to: {GraphicsApi}");

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine Version: {EngineVersion}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW, & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
			Logger.Debug($"- Graphics Api: {GraphicsApi}");

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

			CheckValidStartupSettings(settings);

			if (GraphicsApi != GraphicsApi.Console) {
				Logger.Info("Setting up Toolkit...");
				SetupToolkit(settings.ToolkitOptions!);
				OnSetupToolkitEvent?.Invoke();
			}

			switch (GraphicsApi) {
				case GraphicsApi.OpenGL:
					Logger.Info("Setting up OpenGL...");
					SetupOpenGL();

					break;
				case GraphicsApi.Vulkan:
					Logger.Info("Setting up Vulkan...");
					SetupVulkan(settings.GameName, GameInstance.Version, EngineVersion, settings.VulkanSetting!.DeviceVulkanFeatureCheck); // checked above

					break;
				case GraphicsApi.Console:
				default: break;
			}

			Logger.Debug("Setup finished. Invoking events then entering loop");
			OnSetupFinishedEvent?.Invoke();

			GameLoop();

			Logger.Debug("GameLoop exited naturally");
			OnShutdownEvent?.Invoke();
			Shutdown(0);
		}

		public static void Shutdown(int errorCode) {
			Logger.Debug("Shutdown called");
			shouldRunGameLoop = false;
			Cleanup();
			Environment.Exit(errorCode);
		}

		private static void SetupEngine() { }

		private static void SetupToolkit(ToolkitOptions toolkitOptions) {
			EventQueue.EventRaised += OnEventRaised;

			Toolkit.Init(toolkitOptions);
		}

		// [MustUseReturnValue]
		// private static WindowHandle SetupWindow(StartupSettings settings) {
		// 	const ushort DefaultWidth = 854, DefaultHeight = 480;
		//
		// 	if (settings.GraphicsApiHints == null) { throw new UnreachableException("Checked above"); }
		//
		// 	WindowHandle windowHandle = Toolkit.Window.Create(settings.GraphicsApiHints);
		// 	Toolkit.Window.SetTitle(windowHandle, settings.Title);
		// 	Toolkit.Window.SetSize(windowHandle, new(DefaultWidth, DefaultHeight));
		//
		// 	if (settings.CenterWindow) {
		// 		DisplayHandle handle = Toolkit.Display.OpenPrimary(); // TODO figure out if this is what i am supposed to do
		// 		Toolkit.Display.GetResolution(handle, out int width, out int height);
		// 		Toolkit.Window.SetPosition(windowHandle, new(width / 2 - DefaultWidth / 2, height / 2 - DefaultHeight / 2));
		// 	}
		//
		// 	Windows.Add(new(windowHandle));
		//
		// 	return windowHandle;
		// }

		private static void GameLoop() {
			while (shouldRunGameLoop) {
				Toolkit.Window.ProcessEvents(false);
				if (!shouldRunGameLoop) { break; } // Early exit

				Update();
				GameInstance!.Update(); // shouldn't be null at this point
				UpdateFrame++;

				// RenderContext.PrepareFrame();

				float delta = 0;
				Render(delta);
				GameInstance.Render(delta);

				// RenderContext.FinalizeFrame();
				// RenderContext.SwapBuffers();

				RenderFrame++;

				Thread.Sleep(1); // TODO impl
			}
		}

		private static void Update() { }

		private static void Render(float delta) { }

		private static void CheckValidStartupSettings(StartupSettings settings) {
			switch (GraphicsApi) {
				case GraphicsApi.OpenGL:
					if (settings.ToolkitOptions == null) {
						throw new Engine3Exception("Toolkit cannot be null when using 'OpenGL' GraphicsApi"); //
					} else if (settings.GraphicsApiHints is not OpenGLGraphicsApiHints openglGraphics) {
						throw new Engine3Exception("GraphicsApi was set to 'OpenGL' but the provided GraphicsApiHints was either not of type OpenGLGraphicsApiHints, or null");
					} else if (openglGraphics is not { Version: { Major: 4, Minor: 6, }, Profile: OpenGLProfile.Core, }) {
						throw new Engine3Exception("Engine only supports OpenGL version 4.6 Core"); //
					} else if (settings.OpenGLSettings == null) {
						throw new Engine3Exception("OpenGL Settings cannot be null when using OpenGL"); //
					}

					break;
				case GraphicsApi.Vulkan:
					if (settings.ToolkitOptions == null) {
						throw new Engine3Exception("Toolkit cannot be null when using 'Vulkan' GraphicsApi"); //
					} else if (settings.GraphicsApiHints is not VulkanGraphicsApiHints) {
						throw new Engine3Exception("GraphicsApi was set to 'Vulkan' but the provided GraphicsApiHints was either not of type VulkanGraphicsApiHints, or null"); //
					} else if (settings.VulkanSetting == null) {
						throw new Engine3Exception("Vulkan Settings cannot be null when using Vulkan"); //
					}

					break;
				case GraphicsApi.Console:
				default: break;
			}
		}

		private static void Cleanup() {
			LogManager.Shutdown();

			switch (GraphicsApi) {
				case GraphicsApi.Vulkan: CleanupVulkan(); break;
				case GraphicsApi.OpenGL: CleanupOpenGL(); break;
				case GraphicsApi.Console:
				default: break;
			}
		}

		private static void OnEventRaised(PalHandle? handle, PlatformEventType type, EventArgs args) {
			if (args is CloseEventArgs closeArgs) {
				if (Windows.Find(w => w.WindowHandle == closeArgs.Window) is { } window) {
					window.TryCloseWindow();
					return;
				}

				Logger.Warn("Attempted to close an unknown window");
			}
		}

		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		public class StartupSettings { // TODO split into OpenGL/Vulkan settings? dunno
			public required string GameName { get; init; }
			public required string MainTheadName { get; init; }
			public GraphicsApi GraphicsApi { get; } = GraphicsApi.Console;
			public ToolkitOptions? ToolkitOptions { get; }
			public GraphicsApiHints? GraphicsApiHints { get; }
			public OpenGLSettings? OpenGLSettings { get; }
			public VulkanSettings? VulkanSetting { get; }

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
			public StartupSettings(string gameName, string mainTheadName, ToolkitOptions toolkitOptions, OpenGLGraphicsApiHints graphicsApiHints, OpenGLSettings glSettings) : this(gameName, mainTheadName, toolkitOptions) {
				GraphicsApi = GraphicsApi.OpenGL;
				GraphicsApiHints = graphicsApiHints;
				OpenGLSettings = glSettings;

				toolkitOptions.FeatureFlags = ToolkitFlags.EnableOpenGL;

				graphicsApiHints.Version = new(4, 6);
				graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
				graphicsApiHints.DebugFlag = true;
#endif
			}

			[SetsRequiredMembers]
			public StartupSettings(string title, string mainTheadName, ToolkitOptions toolkitOptions, VulkanGraphicsApiHints graphicsApiHints, VulkanSettings vkSettings) : this(title, mainTheadName, toolkitOptions) {
				GraphicsApi = GraphicsApi.Vulkan;
				GraphicsApiHints = graphicsApiHints;
				VulkanSetting = vkSettings;

				toolkitOptions.FeatureFlags = ToolkitFlags.EnableVulkan;
			}
		}
	}
}