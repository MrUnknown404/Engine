using System.Reflection;
using Engine3.Api;
using Engine3.Api.Graphics;
using Engine3.Exceptions;
using Engine3.Graphics;
using Engine3.IO;
using Engine3.Utils;
using JetBrains.Annotations;
using NLog;
using OpenTK.Platform;

namespace Engine3 {
	[PublicAPI]
	public static class GameEngine {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static readonly Version4 EngineVersion = new(0, 0, 0);

		public static Assembly? EngineAssembly { get; private set; }
		public static Assembly? InstanceAssembly { get; private set; }
		public static IGameClient? GameInstance { get; private set; }
		public static WindowHandle? WindowHandle { get; private set; }
		public static IRenderContext? RenderContext { get; private set; }
		public static GraphicsApiHints? GraphicsApiHints { get; private set; }

		public static string MainThreadName {
			get;
			set {
				field = value;
				mainThread?.Name = value;
			}
		} = "Main";

		public static bool IsCloseRequested {
			get => field || (WindowHandle != null && Toolkit.Window.IsWindowDestroyed(WindowHandle));
			set {
				if (value) {
					Logger.Debug("Close requested");
					if (!GameInstance?.IsCloseAllowed() ?? false) { return; }
				}

				field = value;
				if (field && WindowHandle != null) { Toolkit.Window.Destroy(WindowHandle); }
			}
		}

		public static EngineLoadState CurrentLoadState { get; private set; } = EngineLoadState.None;

		public static ulong Tick { get; private set; }
		public static ulong Frame { get; private set; }
		public static uint Fps { get; private set; }
		public static uint Ups { get; private set; }
		public static double DrawTime { get; private set; }
		public static double UpdateTime { get; private set; }
		public static double TotalFrameTime { get; private set; }

		private static Thread? mainThread;

		public static event Action? OnEngineSetupEvent;
		public static event Action? OnWindowSetupEvent;
		public static event Action? OnGraphicsSetupEvent;
		public static event Action? OnEnginePostGraphicsSetupEvent;
		public static event Action? OnSetupFinishedEvent;
		public static event Action? OnExitEvent;

		public static event Action? ConsoleSetupEvent;
		public static event Action? OnOpenGLSetupEvent;
		public static event Action? OnVulkanSetupEvent;

		public static void Start<T>(IEngineStartupSettings engineSettings) where T : IGameClient, new() {
			mainThread = Thread.CurrentThread;
			mainThread.Name = MainThreadName;

			CurrentLoadState = EngineLoadState.Logger;
			LoggerH.Setup();
			Logger.Debug("Finished setting up NLog");

			RenderContext = engineSettings.RenderContext;
			GraphicsApiHints = engineSettings.GraphicsApiHints;

			if (RenderContext.RenderSystem is RenderSystem.OpenGL or RenderSystem.Vulkan && engineSettings.ToolkitOptions == null) {
				throw new EngineStateException("ToolkitOptions was null when the provided RenderSystem requires it");
			}

			switch (RenderContext.RenderSystem) {
				case RenderSystem.Console: break;
				case RenderSystem.OpenGL:
					if (GraphicsApiHints is not OpenGLGraphicsApiHints openglGraphics) { throw new EngineStateException("RenderSystem was set to OpenGL but the provided GraphicsApiHints was not of type OpenGLGraphicsApiHints"); }
					if (openglGraphics is not { Version: { Major: 4, Minor: 6, }, Profile: OpenGLProfile.Core, }) { throw new EngineStateException("Engine only supports OpenGL version 4.6 Core"); }
					break;
				case RenderSystem.Vulkan:
					if (GraphicsApiHints is not VulkanGraphicsApiHints) { throw new EngineStateException("RenderSystem was set to OpenGL but the provided GraphicsApiHints was not of type VulkanGraphicsApiHints"); }
					break;
				default: throw new ArgumentOutOfRangeException();
			}

			CurrentLoadState = EngineLoadState.Assemblies;
			Logger.Debug("Grabbing assemblies...");
			EngineAssembly = Assembly.GetAssembly(typeof(GameEngine)) ?? throw new NullReferenceException("Unable to get assembly for engine");
			InstanceAssembly = Assembly.GetAssembly(typeof(T)) ?? throw new NullReferenceException("Unable to get assembly for instance");

			CurrentLoadState = EngineLoadState.Engine;
			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine is running version: {EngineVersion}");
			Logger.Debug($"- Engine is using RenderSystem: {RenderContext.RenderSystem}");

			SetupEngine();
			OnEngineSetupEvent?.Invoke();

			CurrentLoadState = EngineLoadState.Game;
			Logger.Debug("Creating game instance...");
			GameInstance = new T();

			Logger.Debug($"- Game is running version: {GameInstance.Version}");
			Logger.Info(GameInstance.StartupMessage);

#if DEBUG
			Logger.Debug("Writing dumps to files outputs...");
			DumpH.WriteDumpsToOutput();
#endif

			CurrentLoadState = EngineLoadState.Graphics;
			if (GraphicsApiHints == null) { throw new NotImplementedException(); } // TODO impl custom console? fully or just use existing?

			if (engineSettings.ToolkitOptions != null) {
				Logger.Info("Setting up toolkit...");
				engineSettings.ToolkitOptions.Logger = null;

				EventQueue.EventRaised += OnEventRaised;
				Toolkit.Init(engineSettings.ToolkitOptions);

				const ushort DefaultWidth = 854, DefaultHeight = 480;

				Logger.Info("Creating window...");
				WindowHandle = Toolkit.Window.Create(GraphicsApiHints);
				Toolkit.Window.SetTitle(WindowHandle, engineSettings.WindowTitle);
				Toolkit.Window.SetSize(WindowHandle, new(DefaultWidth, DefaultHeight));

				if (engineSettings.CenterWindow) {
					DisplayHandle handle = Toolkit.Display.OpenPrimary(); // TODO figure out if this is what i am supposed to do
					Toolkit.Display.GetResolution(handle, out int width, out int height);
					Toolkit.Window.SetPosition(WindowHandle, new(width / 2 - DefaultWidth / 2, height / 2 - DefaultHeight / 2));
				}

				OnWindowSetupEvent?.Invoke();

				Logger.Info($"Setting up graphics... ({RenderContext.RenderSystem})");
				RenderContext.Setup(WindowHandle);

				Logger.Debug($"{RenderContext.RenderSystem} is now ready. Invoking events...");
				switch (RenderContext.RenderSystem) {
					case RenderSystem.Console: ConsoleSetupEvent?.Invoke(); break;
					case RenderSystem.OpenGL: OnOpenGLSetupEvent?.Invoke(); break;
					case RenderSystem.Vulkan: OnVulkanSetupEvent?.Invoke(); break;
					default: throw new ArgumentOutOfRangeException();
				}

				OnGraphicsSetupEvent?.Invoke();

				Logger.Debug("Showing window...");
				Toolkit.Window.SetMode(WindowHandle, WindowMode.Normal);

				CurrentLoadState = EngineLoadState.EnginePostGraphics;
				Logger.Info("Setting up engine post graphics...");
				SetupEnginePostGraphics();
				OnEnginePostGraphicsSetupEvent?.Invoke();
			}

			CurrentLoadState = EngineLoadState.Done;
			Logger.Debug("Setup finished. Invoking events then entering loop");
			OnSetupFinishedEvent?.Invoke();

			GameLoop();
			OnExit(0);
		}

		private static void GameLoop() {
			if (RenderContext == null) { throw new Exception(); } // TODO exception
			if (GameInstance == null) { throw new Exception(); } // TODO exception

			while (!IsCloseRequested) {
				Toolkit.Window.ProcessEvents(false);
				if (IsCloseRequested) { break; } // Early exit

				float delta = 0;

				// TODO imgui goes in here somewhere
				RenderContext.PrepareFrame();
				Render(delta);
				GameInstance.Render(delta);
				RenderContext.SwapBuffers();

				Thread.Sleep(1); // TODO impl
			}
		}

		private static void Update() { }

		private static void Render(float delta) { }

		private static void SetupEngine() { }

		private static void SetupEnginePostGraphics() { }

		private static void OnExit(int errorCode) {
			Logger.Info($"{nameof(OnExit)} called. Shutting down...");
			Logger.Info(GameInstance?.ExitMessage);

			OnExitEvent?.Invoke();
			// TODO cleanup

			LogManager.Shutdown();
			Environment.Exit(errorCode);
		}

		private static void OnEventRaised(PalHandle? handle, PlatformEventType type, EventArgs args) {
			if (args is CloseEventArgs closeArgs) {
				if (closeArgs.Window == WindowHandle) {
					IsCloseRequested = true;
					return;
				}

				Logger.Fatal("Attempted to close an unknown window");
			}
		}
	}
}