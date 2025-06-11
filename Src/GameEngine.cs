using System.Runtime.InteropServices;
using Engine3.Client;
using Engine3.Utils;
using NLog;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GameWindow = Engine3.Client.GameWindow;

// TODO learn multi-threading. how do locks work?

namespace Engine3 {
	public static class GameEngine {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static Version4 EngineVersion { get; } = new(0, 0, 0);

		private static Thread? MainThread { get; set => field = field == null ? value : throw new Exception(); } // TODO exception
		private static Thread? RenderThread { get; set => field = field == null ? value : throw new Exception(); } // TODO exception

		public static GameClient? Instance { get; private set => field = field == null ? value : throw new Exception(); } // TODO exception, clear?
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

		public static string RenderThreadName {
			get;
			set {
				if (value != field) {
					field = value;
					if (RenderThread != null) { RenderThread.Name = field; }
				}
			}
		} = "Render";

		public static bool AddOpenGLCallbacks {
			get;
			set {
				if (Instance != null) {
					Logger.Warn($"Attempted to enable OpenGL Callbacks too late. This must be set before {nameof(GameEngine)}#{nameof(Start)}");
					return;
				}

				field = value;
			}
		}

		public static bool IsCloseRequested { get => field || Window.ShouldClose(); set; }

		public static void Start<T>() where T : GameClient, new() {
			MainThread = Thread.CurrentThread;
			MainThread.Name = MainThreadName;
			RenderThread = new(SetupOpenGL) { Name = RenderThreadName, };
			LoggerH.Setup();

			Logger.Debug($"Engine is running version: {EngineVersion}");
			Logger.Debug("Creating instance...");
			Instance = new T();

			Logger.Info(Instance.StartupMessage);
			Logger.Info($"Running version: {Instance.Version}");

			GLFWProvider.CheckForMainThread = false;

			if (Instance.EnableDebugOutputs) {
				DebugOutputH.Setup();
				Instance.AddDebugOutputs();
			}

			// TODO setup stuff

			Logger.Debug("Starting render thread");
			RenderThread.Start();

			Logger.Debug("Main thread setup done. Waiting for window...");
			while (!Window.IsVisible) { Thread.Sleep(10); }

			Logger.Debug("Window found. Entering loop");
			while (!IsCloseRequested) {
				Instance.Update();
				Thread.Sleep(100);
			}

			OnExit(0);
		}

		private static void SetupOpenGL() {
			Logger.Debug("Creating OpenGL Window");
			Window.CreateOpenGLWindow();
			Logger.Debug("Creating OpenGL Context");
			Window.MakeContextCurrent();

			Logger.Info("Setting up OpenGL");
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
			Instance!.InvokeOnSetupOpenGL();

			Logger.Debug("Enabling window...");
			Window.IsVisible = true;

			Logger.Debug("Render thread setup done. Entering loop");
			RenderLoop();
		}

		private static void RenderLoop() {
			while (!IsCloseRequested) {
				Window.NewInputFrame();

				GL.Clear(GLH.ClearBufferMask);
				Instance!.Render(0);
				Window.SwapBuffers();

				// loop
				Thread.Sleep(100);
			}
		}

		private static void OnExit(int errorCode) {
			while (RenderThread!.IsAlive) { Thread.Sleep(10); } // wait for renderer. this is dumb. i should probably redo this

			Logger.Info(Instance?.ExitMessage);
			LogManager.Shutdown();
			Environment.Exit(errorCode);
		}
	}
}