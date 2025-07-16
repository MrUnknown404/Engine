using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
		[field: MaybeNull] public static Assembly EngineAssembly { get => field ?? throw new Exception(); private set => field = field == null ? value : throw new Exception(); } // TODO exception
		[field: MaybeNull] public static Assembly InstanceAssembly { get => field ?? throw new Exception(); private set => field = field == null ? value : throw new Exception(); } // TODO exception
		public static GameClient? GameInstance { get => field ?? throw new Exception(); private set => field = field == null ? value : throw new Exception(); } // TODO exception
		public static GameWindow Window { get; } = new();

		public static string MainThreadName {
			get;
			set {
				if (value != field) {
					field = value;
					MainThread?.Name = field;
				}
			}
		} = "Main";

		public static bool AddOpenGLCallbacks {
			get;
			set {
				if (WasOpenGLSetupRun) { Logger.Warn($"Attempted to enable OpenGL Callbacks too late. This must be set before calling {nameof(GameEngine)}#{nameof(Start)}"); }
				field = value;
			}
		}

		public static bool EnableDebugOutputs {
			get;
			set {
				if (WasOpenGLSetupRun) { Logger.Warn($"Attempted to enable debug outputs too late. This must be set before calling {nameof(GameEngine)}#{nameof(Start)}"); }
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

		public static bool WasEngineSetupRun { get; private set; }
		public static bool WasOpenGLSetupRun { get; private set; }
		public static bool WasEnginePostOpenGLSetup { get; private set; }

		public static ulong Tick { get; private set; }
		public static ulong Frame { get; private set; }
		public static uint Fps { get; private set; }
		public static uint Ups { get; private set; }
		public static double DrawTime { get; private set; }
		public static double UpdateTime { get; private set; }
		public static double TotalFrameTime { get; private set; }

		public static event Action? OnOpenGLSetupEvent;
		public static event Action? OnSetupFinishedEvent;
		public static event Action? OnExitEvent;

		public static void Start<T>() where T : GameClient, new() {
			MainThread = Thread.CurrentThread;
			MainThread.Name = MainThreadName;

			LoggerH.Setup();

			Logger.Debug("Grabbing assemblies...");
			EngineAssembly = Assembly.GetAssembly(typeof(GameEngine)) ?? throw new NullReferenceException("Unable to get assembly for engine.");
			InstanceAssembly = Assembly.GetAssembly(typeof(T)) ?? throw new NullReferenceException("Unable to get assembly for instance.");

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine is running version: {EngineVersion}");
			SetupEngine();
			WasEngineSetupRun = true;

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
			OnOpenGLSetupEvent?.Invoke();
			WasOpenGLSetupRun = true;

			Logger.Info("Setting up engine post OpenGL...");
			EnginePostOpenGLSetup();
			WasEnginePostOpenGLSetup = true;

			Logger.Debug("Setup finished. Invoking events then entering loop");
			OnSetupFinishedEvent?.Invoke();
			GameLoop();

			OnExit(0);
		}

		private static void GameLoop() {
			if (GameInstance == null) { throw new NullReferenceException("how did we get here?"); }

			const byte FrameGoal = 60;
			const double UpdatePeriodMs = 1000d / FrameGoal;
			const int FrameSkip = 5;

			double time = GetTime();
			double frameTimer = 0;
			uint fpsCounter = 0;
			uint upsCounter = 0;

			static double GetTime() => GLFW.GetTime() * 1000d;

			while (!IsCloseRequested) {
				double startTime = GetTime();

				Window.NewInputFrame();

				int loops = 0;
				while (GetTime() > time && loops < FrameSkip) {
					double updateTime = GetTime();

					Update();
					GameInstance.Update();

					time += UpdatePeriodMs;
					loops++;
					upsCounter++;
					Tick++;

					UpdateTime = GetTime() - updateTime;
				}

				if (loops >= FrameSkip) { Logger.Warn("Too many frame skips detected? handle?"); }
				double drawTime = GetTime();

				GL.Clear(GLH.ClearBufferMask);
				GameInstance.Render((float)((drawTime + UpdatePeriodMs - time) / UpdatePeriodMs));
				Window.SwapBuffers();

				DrawTime = GetTime() - drawTime;
				TotalFrameTime = GetTime() - startTime;
				frameTimer += TotalFrameTime;
				fpsCounter++;
				Frame++;

				if (frameTimer >= 1000) {
					Fps = fpsCounter;
					Ups = upsCounter;
					fpsCounter = 0;
					upsCounter = 0;
					frameTimer -= 1000;
				}
			}
		}

		private static void Update() {
			// TODO impl
		}

		private static void SetupEngine() {
			// TODO impl
		}

		private static void SetupOpenGL() {
			if (GameInstance == null) { throw new NullReferenceException("how did we get here?"); }

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
			OnOpenGLSetupEvent?.Invoke();

			Logger.Debug("Enabling window...");
			Window.IsVisible = true;
		}

		private static void EnginePostOpenGLSetup() {
			if (GameInstance == null) { throw new NullReferenceException("how did we get here?"); }

			Logger.Debug("Registering shaders...");
			HashSet<Shader> set = GameInstance.ShadersToRegister.Value;
			foreach (Shader shader in set) { shader.SetupGL(); }
			Logger.Debug($"Finished registering {set.Count} shaders");
		}

		private static void OnExit(int errorCode) {
			Logger.Info($"{nameof(OnExit)} called. Shutting down...");
			Logger.Info(GameInstance?.ExitMessage);

			OnExitEvent?.Invoke();
			// TODO cleanup

			LogManager.Shutdown();
			Environment.Exit(errorCode);
		}
	}
}