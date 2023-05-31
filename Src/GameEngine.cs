using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client;
using USharpLibs.Engine.Client.Fonts;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.UI;
using USharpLibs.Engine.Init;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine {
	[PublicAPI]
	public abstract class GameEngine {
		private static GameEngine instance = default!;

		protected static T Instance<T>() where T : GameEngine => (T)instance;
		private static Lazy<Type> InstanceType { get; } = new(() => instance.GetType());
		public static Lazy<Assembly> InstanceAssembly { get; } = new(() => Assembly.GetAssembly(InstanceType.Value) ?? throw new Exception("Assembly cannot be found."));
		public static Lazy<Assembly> USharpEngineAssembly { get; } = new(() => Assembly.GetAssembly(typeof(GameEngine)) ?? throw new Exception("Assembly cannot be found."));

		protected internal event Action<EngineWindow>? WindowCreationEvent;
		protected internal event Action<WindowState>? FullscreenToggleEvent;
		protected internal event Func<HashSet<IUnboundShader>>? ShaderCreationEvent;
		protected internal event Func<HashSet<RawFont>>? FontCreationEvent;
		protected internal event Func<HashSet<Texture>>? TextureCreationEvent;
		protected internal event Func<HashSet<Screen>>? ScreenCreationEvent;

		protected internal event Action? OnSetupLoadingScreenEvent;
		protected internal event Action? OnClosingEvent;
		protected event Action? OnFontsFinishedEvent;
		protected event Action? OnTexturesFinishedEvent;
		protected event Action? OnShadersFinishedEvent;
		protected event Action? OnScreensFinishedEvent;
		protected event Action? OnRenderersFinishedEvent;
		protected event Action? OnSetupFinishedEvent;

		protected internal event Action<KeyboardKeyEventArgs>? OnKeyPressEvent;
		protected internal event Action<KeyboardKeyEventArgs>? OnKeyReleaseEvent;
		protected internal event Action<MouseMoveEventArgs>? OnMouseMoveEvent;
		protected internal event Action<MouseButtonEventArgs>? OnMousePressEvent;
		protected internal event Action<MouseButtonEventArgs>? OnMouseReleaseEvent;
		protected internal event Action<MouseWheelEventArgs>? OnMouseScrollEvent;

		private static EngineWindow Window { get; set; } = default!;
		public static LoadState LoadState { get; protected internal set; } = LoadState.NotStarted;
		public static bool IsDebug { get; protected set; }
		public static bool CloseRequested { get; internal set; } // I don't like this but GameWindow#IsExiting doesn't seem to work sometimes
		public static Screen? CurrentScreen { get; set; }

		internal static uint RawFPS, RawTPS;
		internal static double RawFrameFrequency, RawTickFrequency;

		public static uint FPS => RawFPS;
		public static uint TPS => RawTPS;
		public static double FrameFrequency => RawFrameFrequency;
		public static double TickFrequency => RawTickFrequency;

		private List<IRenderer> Renderers { get; } = new();
		private HashSet<Screen> Screens { get; } = new();

		public string OriginalTitle { get; }
		protected internal bool ShouldScreenCheckCancelMouseEvent { get; set; } = true;
		protected ushort MaxAmountOfLogs { private get; set; } = 5;

		public static ushort MouseX { get; set; }
		public static ushort MouseY { get; set; }

		private string title;
		private ushort minWidth;
		private ushort minHeight;
		private ushort maxWidth;
		private ushort maxHeight;

		public string Title {
			get => title;
			protected set {
				title = value;
				Window.Title = title;
			}
		}

		public ushort MinWidth {
			get => minWidth;
			protected set {
				minWidth = value;
				Window.MinimumSize = new(minWidth, MinHeight);
			}
		}

		public ushort MinHeight {
			get => minHeight;
			protected set {
				minHeight = value;
				Window.MinimumSize = new(MinWidth, minHeight);
			}
		}

		public ushort MaxWidth {
			get => maxWidth;
			protected set {
				maxWidth = value;
				Window.MaximumSize = new(maxWidth, MaxHeight);
			}
		}

		public ushort MaxHeight {
			get => maxHeight;
			protected set {
				maxHeight = value;
				Window.MaximumSize = new(MaxWidth, maxHeight);
			}
		}

		public static ushort Width => (ushort)Window.Size.X;
		public static ushort Height => (ushort)Window.Size.Y;

		protected GameEngine(string title, ushort minWidth, ushort minHeight, ushort maxWidth, ushort maxHeight, bool isDebug = false) {
			OriginalTitle = title;
			this.title = title;
			this.minWidth = minWidth;
			this.minHeight = minHeight;
			this.maxWidth = maxWidth;
			this.maxHeight = maxHeight;
			IsDebug = isDebug;

			Logger.LogLevel = LogLevel.More;
			Logger.SetupDefaultLogFolder(5, $"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");

			ShaderCreationEvent += () => DefaultShaders.AllShaders;

			OnMouseMoveEvent += e => {
				MouseX = (ushort)MathH.Floor(e.X);
				MouseY = (ushort)MathH.Floor(e.Y);
				CurrentScreen?.CheckForHover(MouseX, MouseY);
			};
		}

		protected GameEngine(string title, ushort minWidth, ushort minHeight, bool isDebug = false) : this(title, minWidth, minHeight, 0, 0, isDebug) { }
		protected GameEngine(string title, bool isDebug = false) : this(title, 856, 482, 0, 0, isDebug) { }

		public static void Start(GameEngine instance) {
			LoadState = LoadState.PreInit;
			using (Window = new(GameEngine.instance = instance)) {
				LoadState = LoadState.Init;

				if (instance.ScreenCreationEvent != null) {
					foreach (Delegate d in instance.ScreenCreationEvent.GetInvocationList()) { instance.Screens.UnionWith((HashSet<Screen>)(d.DynamicInvoke() ?? new HashSet<Screen>())); }
				}

				Logger.Debug($"Running Init took {TimeH.Time(instance.Init).Milliseconds}ms");
				Window.Run();
			}

			Logger.Info("Goodbye!");
		}

		internal void InvokeWindowCreationEvent(EngineWindow window) => WindowCreationEvent?.Invoke(window);
		internal void InvokeOnSetupLoadingScreenEvent() => OnSetupLoadingScreenEvent?.Invoke();
		internal void InvokeOnSetupFinishEvent() => OnSetupFinishedEvent?.Invoke();
		internal void InvokeOnClosingEvent() => OnClosingEvent?.Invoke();

		internal void InvokeOnKeyPressEvent(KeyboardKeyEventArgs e) => OnKeyPressEvent?.Invoke(e);
		internal void InvokeOnKeyReleaseEvent(KeyboardKeyEventArgs e) => OnKeyReleaseEvent?.Invoke(e);
		internal void InvokeOnMouseMoveEvent(MouseMoveEventArgs e) => OnMouseMoveEvent?.Invoke(e);
		internal void InvokeOnMousePressEvent(MouseButtonEventArgs e) => OnMousePressEvent?.Invoke(e);
		internal void InvokeOnMouseReleaseEvent(MouseButtonEventArgs e) => OnMouseReleaseEvent?.Invoke(e);
		internal void InvokeOnMouseScrollEvent(MouseWheelEventArgs e) => OnMouseScrollEvent?.Invoke(e);

		protected virtual void Init() { }

		internal static void CreateGL() {
			Logger.Info($"Setting up OpenGL! Running OpenGL version: {OpenGL4.GetString(StringName.Version)}");
			OpenGL4.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			OpenGL4.Enable(EnableCap.Blend);
			OpenGL4.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();
		}

		protected internal virtual void SetupGL() {
			HashSet<IUnboundShader> shaders = new();
			HashSet<RawFont> fonts = new();
			HashSet<Texture> textures = new();

			if (ShaderCreationEvent != null) {
				foreach (Delegate d in ShaderCreationEvent.GetInvocationList()) { shaders.UnionWith((HashSet<IUnboundShader>)(d.DynamicInvoke() ?? new HashSet<IUnboundShader>())); }
			}

			if (FontCreationEvent != null) {
				foreach (Delegate d in FontCreationEvent.GetInvocationList()) { fonts.UnionWith((HashSet<RawFont>)(d.DynamicInvoke() ?? new HashSet<RawFont>())); }
			}

			if (TextureCreationEvent != null) {
				foreach (Delegate d in TextureCreationEvent.GetInvocationList()) { textures.UnionWith((HashSet<Texture>)(d.DynamicInvoke() ?? new HashSet<Texture>())); }
			}

			void Fonts() =>
					fonts.ForEach(f => {
						if (f.Setup() is { } texture) { textures.Add(texture); }
					});

			void Shaders() =>
					shaders.ForEach(s => {
						s.SetupGL();
						Window.Resize += s.OnResize;
					});

			if (fonts.Count != 0) { Logger.Debug($"Setting up {fonts.Count} fonts took {TimeH.Time(Fonts).Milliseconds}ms"); }
			OnFontsFinishedEvent?.Invoke();

			// Java lets me do this nicer ): `Class::Method`
			if (textures.Count != 0) { Logger.Debug($"Setting up {textures.Count} textures took {TimeH.Time(() => textures.ForEach(t => t.SetupGL())).Milliseconds}ms"); }
			OnTexturesFinishedEvent?.Invoke();

			if (shaders.Count != 0) { Logger.Debug($"Setting up {shaders.Count} shaders took {TimeH.Time(Shaders).Milliseconds}ms"); }
			OnShadersFinishedEvent?.Invoke();

			if (Screens.Count != 0) { Logger.Debug($"Setting up {Screens.Count} screens took {TimeH.Time(() => Screens.ForEach(r => r.SetupGL())).Milliseconds}ms"); }
			OnScreensFinishedEvent?.Invoke();

			AddRenderers(Renderers);
			if (Renderers.Count != 0) { Logger.Debug($"Setting up {Renderers.Count} renderers took {TimeH.Time(() => Renderers.ForEach(r => r.SetupGL())).Milliseconds}ms"); }
			OnRenderersFinishedEvent?.Invoke();
		}

		public virtual void Tick(double time) { }
		public virtual void Render(double time) => Renderers.ForEach(r => r.Render(time));

		/// <returns> <c>true</c> if mouse event should be canceled. Otherwise <c>false</c>. </returns>
		protected internal virtual bool CheckScreenMouseRelease(MouseButtonEventArgs e) =>
				e.Button is MouseButton.Left or MouseButton.Right && e.Action != InputAction.Repeat && (CurrentScreen?.CheckForRelease(e.Button, MouseX, MouseY) ?? false);

		/// <returns> <c>true</c> if mouse event should be canceled. Otherwise <c>false</c>. </returns>
		protected internal virtual bool CheckScreenMousePress(MouseButtonEventArgs e) {
			if (e.Action != InputAction.Repeat) {
				if (e.Button is MouseButton.Left or MouseButton.Right) { CurrentScreen?.CheckForFocus(MouseX, MouseY); }
				return CurrentScreen?.CheckForPress(e.Button, MouseX, MouseY) ?? false;
			}

			return false;
		}

		protected abstract void AddRenderers(List<IRenderer> renderers);

		public void ToggleFullscreen() {
			Window.WindowState = Window.WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
			FullscreenToggleEvent?.Invoke(Window.WindowState);
		}

		public static void ForceInLoadState(LoadState loadState, Action todo) {
			LoadState old = LoadState;
			LoadState = loadState;
			todo();
			LoadState = old;
		}
	}

	public enum LoadState {
		NotStarted = 0,
		PreInit,
		Init,
		CreateGL,
		SetupGL,
		Done,
	}
}