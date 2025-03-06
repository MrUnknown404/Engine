using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.IO;
using USharpLibs.Engine2.Client;
using USharpLibs.Engine2.Client.Shaders;
using USharpLibs.Engine2.Events;
using USharpLibs.Engine2.Exceptions;
using USharpLibs.Engine2.Init;
using USharpLibs.Engine2.Modding;

namespace USharpLibs.Engine2 {
	// TODO completely redo this
	// FPS, Min. Max, Avg
	// TPS, Min. Max, Avg
	// also draw/tick times

	public abstract partial class GameEngine {
		internal static ModVersion EngineVersion { get; } = new() { Release = 0, Major = 0, Minor = 0, };
		internal static HashSet<Shader> AllShaders { get; } = new();

		[field: MaybeNull]
		public static ModSource EngineSource {
			get => field ?? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled);
			internal set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		[field: MaybeNull]
		public static ModSource InstanceSource {
			get => field ?? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled);
			internal set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		[field: MaybeNull]
		public static GameEngine Instance {
			get => field == null ? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled) : field;
			internal set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		[field: MaybeNull]
		public static EngineWindow WindowInstance {
			get => field == null ? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled) : field;
			internal set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		public static uint FPS => WindowInstance.FPS;
		public static double FrameFrequency => WindowInstance.FrameFrequency;

		public static bool IsCloseRequested {
			get {
				unsafe { return GLFW.WindowShouldClose(WindowInstance.WindowPtr) || field; }
			}
			private set;
		}

		protected static event EventResultDelegate<OnRequestCloseEvent>? OnRequestClose;

		internal GameEngine() { }

		protected internal virtual void OnUpdate(double time) { }
		protected internal virtual void OnRender(double time) { }

		public static void RequestClose(bool force = false) {
			if (force) {
				IsCloseRequested = true;
				Logger.Debug("Close requested forced.");
				return;
			}

			OnRequestCloseEvent result = InvokeEvent(OnRequestClose);
			Logger.Debug($"Close requested. ShouldClose: {result}");
			IsCloseRequested = result.ShouldClose;
		}

		internal static void InvokeEvent(EventResultDelegate? e) => e?.Invoke();

		[MustUseReturnValue]
		internal static T InvokeEvent<T>(EventResultDelegate<T>? e) where T : IEventResult, new() {
			if (e == null) { return (T)T.Empty; }

			T result = new();
			e.Invoke(result);
			return result;
		}

		public delegate void EventResultDelegate();
		public delegate void EventResultDelegate<in T>(T r) where T : IEventResult, new();

		[LibraryImport("Kernel32.dll", SetLastError = true)]
		internal static partial void SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		[LibraryImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

		[LibraryImport("Kernel32.dll", SetLastError = true)]
		internal static partial IntPtr GetStdHandle(uint nStdHandle);

		public sealed class StartupInfo {
			public required ModVersion Version { get; init; }
			public HashSet<Shader>? ShadersToRegister { get; init; }
			public bool AddOpenGLCallbacks { get; init; }
		}
	}

	public abstract class GameEngine<TSelf> : GameEngine where TSelf : GameEngine<TSelf>, new() {
		public new static TSelf Instance => (TSelf)GameEngine.Instance;

		protected static event EventResultDelegate? OnWindowReady;
		protected static event EventResultDelegate? OnOpenGLReady;
		protected static event EventResultDelegate? OnShadersReady;
		protected static event EventResultDelegate? OnSetupFinished;

		protected static void Start(StartupInfo info) {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				IntPtr handle = GetStdHandle(unchecked((uint)-11)); // i have no idea where this magic number comes from
				GetConsoleMode(handle, out uint mode);
				SetConsoleMode(handle, mode | 0x0004);
			}

			Thread.CurrentThread.Name = "Main";
			Logger.Init($"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");
			Logger.Debug($"Logs -> {Logger.LogDirectory}");

			Logger.Debug($"Engine is running version: {EngineVersion}");
			Logger.Debug($"Instance is running version: {info.Version}");

			Logger.Debug("Creating sources...");
			EngineSource = new(Assembly.GetAssembly(typeof(GameEngine)) ?? throw new NullReferenceException("Unable to get assembly for engine."), EngineVersion);
			InstanceSource = new(Assembly.GetAssembly(typeof(TSelf)) ?? throw new NullReferenceException("Unable to get assembly for instance."), info.Version);

			Logger.Debug("Creating self instance...");
			GameEngine.Instance = new TSelf();

			Logger.Debug("Creating window instance...");
			WindowInstance = new();
			InvokeEvent(OnWindowReady);

			Logger.Debug("Creating OpenGL window...");
			WindowInstance.CreateOpenGLWindow(info);
			Logger.Debug("Making sure OpenGL context is current...");
			WindowInstance.MakeContextCurrent(); // don't know if this is necessary

			Logger.Debug("OpenGL is now ready. Invoking events...");
			OpenGLReady(info);
			InvokeEvent(OnOpenGLReady);

			// TODO trace & time
			InitOpenGLObjects(info);
			InitEngineObjects();

			Logger.Debug("Engine finished initializing. Invoking events...");
			InvokeEvent(OnSetupFinished);
			Logger.Info("Engine initialization finished!");

			Logger.Debug("Starting game-loop...");
			WindowInstance.Run();
			Logger.Info("Goodbye!");
		}

		private static void OpenGLReady(StartupInfo info) {
			Logger.Info("Setting up OpenGL!");
			Logger.Debug($"- OpenGL version: {GL.GetString(StringName.Version)}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}");

			if (info.AddOpenGLCallbacks) {
				Logger.Debug("- OpenGL Debug Flag is Enabled!");

				GL.Enable(EnableCap.DebugOutput);
				GL.Enable(EnableCap.DebugOutputSynchronous);

				uint[] ids = [
						131185, // Nvidia static buffer notification
				];

				GL.DebugMessageControl(DebugSourceControl.DebugSourceApi, DebugTypeControl.DebugTypeOther, DebugSeverityControl.DontCare, ids.Length, ids, false);

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
		}

		private static void InitOpenGLObjects(StartupInfo info) {
			// TODO setup loading screen
			// TODO setup fonts
			// TODO setup renderers

			// TODO time and trace

			SetupShaders(info);
			InvokeEvent(OnShadersReady);
		}

		private static void InitEngineObjects() {
			// TODO init
		}

		private static void SetupShaders(StartupInfo info) {
			Logger.Debug("Setting up shaders...");
			Stopwatch w = new();
			w.Start();

			HashSet<Shader> shaders = info.ShadersToRegister ?? new();

			AllShaders.UnionWith(DefaultShaders.AllShaders);
			AllShaders.UnionWith(shaders);

			foreach (Shader shader in AllShaders) { shader.SetupGL(); }

			w.Stop();
			Logger.Debug($"Setting up {AllShaders.Count} shaders took {(uint)w.ElapsedMilliseconds}ms");
		}
	}
}