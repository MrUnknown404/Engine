using System.Runtime.InteropServices;
using Engine3.Client;
using Engine3.Utils;
using NLog;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine3 {
	public static class GameEngine {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static Version4 EngineVersion { get; } = new(0, 0, 0);

		private static Thread? MainThread { get; set => field = field == null ? value : throw new Exception(); } // TODO exception

		public static GameClient? GameInstance { get; private set => field = field == null ? value : throw new Exception(); } // TODO exception, clear?
		public static GameWindow Window { get; } = new(); // TODO exception

		public static string MainThreadName {
			get;
			set {
				if (value != field) {
					field = value;
					//MainThread?.Name = field; // TODO when rider updates
					if (MainThread != null) { MainThread.Name = field; }
				}
			}
		} = "Main";

		public static bool AddOpenGLCallbacks {
			get;
			set {
				if (GameInstance != null) { Logger.Warn($"Attempted to enable OpenGL Callbacks too late. This must be set before calling {nameof(GameEngine)}#{nameof(Start)}"); }
				field = value;
			}
		}

		public static bool EnableDebugOutputs {
			get;
			set {
				if (GameInstance != null) { Logger.Warn($"Attempted to enable debug outputs too late. This must be set before calling {nameof(GameEngine)}#{nameof(Start)}"); }
				field = value;
			}
		}

		public static bool IsCloseRequested {
			get => (field || Window.ShouldClose()) && (GameInstance?.IsCloseAllowed() ?? false);
			set {
				if (value) { Logger.Debug("Close requested"); }
				field = value;
			}
		}

		public static void Start<T>() where T : GameClient, new() {
			MainThread = Thread.CurrentThread;
			MainThread.Name = MainThreadName;
			LoggerH.Setup();

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine is running version: {EngineVersion}");
			SetupEngine();

			Logger.Debug("Creating game instance...");
			GameInstance = new T();

			Logger.Debug($"- Game is running version: {GameInstance.Version}");
			Logger.Info(GameInstance.StartupMessage);

			if (EnableDebugOutputs) {
				Logger.Debug("Setting up debug outputs...");
				DebugOutputH.Setup(GameInstance);
			}

			Logger.Info("Setting up OpenGL...");
			SetupOpenGL();
			SetupEnginePostOpenGL();

			Logger.Info("Setup finished. Entering loop");
			GameLoop();
			OnExit(0);
		}

		private static void GameLoop() {
			if (GameInstance == null) { throw new NullReferenceException("how did we get here?"); }

			while (!IsCloseRequested) {
				Window.NewInputFrame();

				GL.Clear(GLH.ClearBufferMask);

				GameInstance.Update();
				GameInstance.Render(0);

				Window.SwapBuffers();

				Thread.Sleep(100); // TODO loop properly
			}
		}

		private static void SetupEngine() {
			// TODO impl
		}

		private static void SetupOpenGL() {
			Logger.Debug("Creating OpenGL context and window...");
			Window.CreateOpenGLWindow();
			Window.MakeContextCurrent();

			Logger.Info("OpenGL context created");
			Logger.Debug($"- OpenGL version: {GL.GetString(StringName.Version)}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}");

			if (AddOpenGLCallbacks) {
				Logger.Debug("- OpenGL Callbacks are enabled");

				GL.Enable(EnableCap.DebugOutput);
				GL.Enable(EnableCap.DebugOutputSynchronous);

				uint[] defaultIds = [
						131185, // Nvidia static buffer notification
				];

				GL.DebugMessageControl(DebugSourceControl.DebugSourceApi, DebugTypeControl.DebugTypeOther, DebugSeverityControl.DontCare, defaultIds.Length, defaultIds, false);

				GL.DebugMessageCallback(static (source, type, id, severity, length, message, _) => {
					switch (severity) {
						case DebugSeverity.DontCare: return;
						case DebugSeverity.DebugSeverityNotification:
							Logger.Debug($"OpenGL Notification: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Debug($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						case DebugSeverity.DebugSeverityHigh:
							Logger.Fatal($"OpenGL Fatal Error: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Fatal($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						case DebugSeverity.DebugSeverityMedium:
							Logger.Error($"OpenGL Error: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Error($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						case DebugSeverity.DebugSeverityLow:
							Logger.Warn($"OpenGL Warning: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Warn($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						default: throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
					}
				}, IntPtr.Zero);
			}

			GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			GLH.EnableBlend(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();

			Logger.Debug("OpenGL is now ready. Invoking events...");
			GameInstance!.InvokeOnSetupOpenGL();

			Logger.Debug("Enabling window...");
			Window.IsVisible = true;
		}

		private static void SetupEnginePostOpenGL() {
			// TODO impl
		}

		private static void OnExit(int errorCode) {
			Logger.Info($"{nameof(OnExit)} called. Shutting down...");
			Logger.Info(GameInstance?.ExitMessage);

			// TODO cleanup

			LogManager.Shutdown();
			Environment.Exit(errorCode);
		}
	}
}