using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.IO;
using USharpLibs.Common.Utils;
using USharpLibs.Engine2.Client;
using USharpLibs.Engine2.Client.GL;
using USharpLibs.Engine2.Client.GL.Shaders;
using USharpLibs.Engine2.Engine;
using USharpLibs.Engine2.Init;
using USharpLibs.Engine2.Modding;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

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

		public static bool IsCloseRequested {
			get {
				unsafe { return GLFW.WindowShouldClose(WindowInstance.WindowPtr) || field; }
			}
			private set;
		}

		public static uint FPS => WindowInstance.FPS;
		public static double FrameFrequency => WindowInstance.FrameFrequency;

		protected Func<HashSet<Shader>>? InstanceShaders { get; init; }

		private static HashSet<Shader> AllShaders { get; } = new();

		internal GameEngine() { }

		internal void SetupShaders() {
			TimeH.Start();

			Logger.Debug("Setting up shaders...");
			AllShaders.UnionWith(DefaultShaders.AllShaders);
			AllShaders.UnionWith(InstanceShaders?.Invoke() ?? new());

			foreach (Shader shader in AllShaders) { shader.SetupGL(); }

			TimeSpan time = TimeH.Stop();
			Logger.Debug($"Setting up {AllShaders.Count} shaders took {time.Milliseconds}ms");
		}

		protected internal virtual void OnUpdate(double time) { }
		protected internal virtual void OnRender(double time) { }

		protected virtual void SetupWindow() { }

		protected virtual void SetupOpenGL() {
			OpenGL4.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			OpenGL4.Enable(EnableCap.Blend);
			OpenGL4.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
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

		protected static void Start() {
			Thread.CurrentThread.Name = "Main";
			Logger.Init($"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");
			Logger.Debug($"Logs -> {Logger.LogDirectory}");

			Logger.Debug("Creating engine instance...");
			EngineSource = new(Assembly.GetAssembly(typeof(GameEngine)) ?? throw new NullReferenceException("Unable to get assembly for engine."));
			InstanceSource = new(Assembly.GetAssembly(typeof(TSelf)) ?? throw new NullReferenceException("Unable to get assembly for instance."));
			EngineInstance = new TSelf();

			Logger.Debug("Creating window instance...");
			WindowInstance = new();
			SelfInstance.SetupWindow();
			WindowInstance.CreateOpenGLWindow();

			Logger.Debug("Creating OpenGL context...");
			WindowInstance.MakeContextCurrent();

			Logger.Info("Setting up OpenGL!");
			Logger.Debug($"- OpenGL version: {OpenGL4.GetString(StringName.Version)}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}");

			SelfInstance.SetupOpenGL();

			// TODO do stuff here
			// support loading screen
			// shaders/textures

			SelfInstance.SetupShaders();

			Logger.Debug("Starting game-loop...");
			WindowInstance.Run();

			Logger.Info("Goodbye!");
		}
	}
}