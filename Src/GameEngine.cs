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
	}

	public abstract class GameEngine<TSelf> : GameEngine where TSelf : GameEngine<TSelf>, new() {
		public new static TSelf Instance => (TSelf)GameEngine.Instance;

		protected static event EventResultDelegate? OnWindowCreation;
		protected static event EventResultDelegate? OnOpenGLReady;
		protected static event EventResultDelegate? OnShadersReady;
		protected static event EventResultDelegate? OnSetupFinished;

		protected static void Start(StartInfo info) {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				IntPtr handle = GetStdHandle(unchecked((uint)-11));
				GetConsoleMode(handle, out uint mode);
				SetConsoleMode(handle, mode | 0x0004);
			}

			Thread.CurrentThread.Name = "Main";
			Logger.Init($"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");
			Logger.Debug($"Logs -> {Logger.LogDirectory}");

			Logger.Debug("Creating source and instance...");
			EngineSource = new(Assembly.GetAssembly(typeof(GameEngine)) ?? throw new NullReferenceException("Unable to get assembly for engine."), EngineVersion);
			InstanceSource = new(Assembly.GetAssembly(typeof(TSelf)) ?? throw new NullReferenceException("Unable to get assembly for instance."), info.Version);
			GameEngine.Instance = new TSelf();

			Logger.Debug("Creating window instance...");
			WindowInstance = new();
			InvokeEvent(OnWindowCreation);

			Logger.Debug("Creating OpenGL window...");
			WindowInstance.CreateOpenGLWindow();
			Logger.Debug("Making sure OpenGL context is current...");
			WindowInstance.MakeContextCurrent();

			Logger.Debug("OpenGL is now ready. Invoking events...");
			OpenGLReady();
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

		private static void OpenGLReady() {
			Logger.Info("Setting up OpenGL!");
			Logger.Debug($"- OpenGL version: {GL.GetString(StringName.Version)}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}");

			GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();
		}

		private static void InitOpenGLObjects(StartInfo info) {
			// TODO setup loading screen
			// TODO setup fonts
			// TODO setup renderers

			// TODO time and trace

			SetupObject(info, "shaders", SetupShaders);
			InvokeEvent(OnShadersReady);
		}

		private static void InitEngineObjects() {
			// TODO init
		}

		private static void SetupObject(StartInfo info, string name, SetupObjectsDelegate toRun) {
			Logger.Debug($"Setting up {name}...");

			Stopwatch w = new();
			w.Start();
			uint count = toRun(info);
			w.Stop();

			Logger.Debug($"Setting up {count} {name} took {(uint)w.ElapsedMilliseconds}ms");
		}

		private static uint SetupShaders(StartInfo info) {
			HashSet<Shader> shaders = info.Shaders?.Invoke() ?? new();

			AllShaders.UnionWith(DefaultShaders.AllShaders);
			AllShaders.UnionWith(shaders);

			foreach (Shader shader in AllShaders) { shader.SetupGL(); }
			return (uint)AllShaders.Count;
		}

		private delegate uint SetupObjectsDelegate(StartInfo info);

		public sealed class StartInfo {
			public required ModVersion Version { get; init; }
			public Func<HashSet<Shader>>? Shaders { get; init; } // TODO i don't like this
		}
	}
}