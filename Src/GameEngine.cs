using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.IO;
using USharpLibs.Common.Utils;
using USharpLibs.Engine2.Client;
using USharpLibs.Engine2.Client.Shaders;
using USharpLibs.Engine2.Exceptions;
using USharpLibs.Engine2.Init;
using USharpLibs.Engine2.Modding;
using USharpLibs.Engine2.Utils;

namespace USharpLibs.Engine2 {
	[PublicAPI]
	public abstract class GameEngine {
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
		public static GameEngine EngineInstance {
			get => field == null ? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled) : field;
			internal set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		[field: MaybeNull]
		public static EngineWindow WindowInstance {
			get => field == null ? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled) : field;
			internal set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		internal static ModVersion EngineVersion { get; } = new() { Release = 0, Major = 0, Minor = 0, };

		public static bool IsCloseRequested {
			get {
				unsafe { return GLFW.WindowShouldClose(WindowInstance.WindowPtr) || field; }
			}
			private set;
		}

		public static uint FPS => WindowInstance.FPS;
		public static double FrameFrequency => WindowInstance.FrameFrequency;

		internal static HashSet<Shader> AllShaders { get; } = new();

		protected ModVersion InstanceVersion { get; init; }

		internal GameEngine() { }

		protected internal virtual void OnUpdate(double time) { }
		protected internal virtual void OnRender(double time) { }

		protected virtual void SetupWindow() { }

		protected virtual void SetupOpenGL() {
			GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();
		}

		protected virtual bool AllowCloseRequest() => true;

		public static void RequestClose(bool force = false) {
			if (force) {
				IsCloseRequested = true;
				return;
			}

			IsCloseRequested = EngineInstance.AllowCloseRequest();
		}
	}

	[PublicAPI]
	public abstract class GameEngine<TSelf> : GameEngine where TSelf : GameEngine<TSelf>, new() {
		public static TSelf SelfInstance => (TSelf)EngineInstance;

		protected static void Start(StartInfo info) {
			Thread.CurrentThread.Name = "Main";
			Logger.Init($"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");
			Logger.Debug($"Logs -> {Logger.LogDirectory}");

			Logger.Debug("Creating engine instance...");
			EngineSource = new(Assembly.GetAssembly(typeof(GameEngine)) ?? throw new NullReferenceException("Unable to get assembly for engine."), EngineVersion);
			InstanceSource = new(Assembly.GetAssembly(typeof(TSelf)) ?? throw new NullReferenceException("Unable to get assembly for instance."), info.Version);
			EngineInstance = new TSelf();

			Logger.Debug("Creating window instance...");
			WindowInstance = new();
			SelfInstance.SetupWindow();
			WindowInstance.CreateOpenGLWindow();

			Logger.Debug("Creating OpenGL context...");
			WindowInstance.MakeContextCurrent();

			Logger.Info("Setting up OpenGL!");
			Logger.Debug($"- OpenGL version: {GL.GetString(StringName.Version)}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}");

			SelfInstance.SetupOpenGL();

			// TODO do stuff here
			// support loading screen
			// shaders/textures

			SetupShaders(info.Shaders?.Invoke());

			Logger.Debug("Starting game-loop...");
			WindowInstance.Run();

			Logger.Info("Goodbye!");
		}

		private static void SetupShaders(HashSet<Shader>? shaders) {
			TimeH.Start();

			Logger.Debug("Setting up shaders...");
			AllShaders.UnionWith(DefaultShaders.AllShaders);
			if (shaders != null) { AllShaders.UnionWith(shaders); }

			foreach (Shader shader in AllShaders) { shader.SetupGL(); }

			TimeSpan time = TimeH.Stop();
			Logger.Debug($"Setting up {AllShaders.Count} shaders took {time.Milliseconds}ms");
		}

		public sealed class StartInfo {
			public required ModVersion Version { get; init; }
			public Func<HashSet<Shader>>? Shaders { get; init; }
		}
	}
}