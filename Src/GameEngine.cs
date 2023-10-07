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
	/// <summary> The main engine class. To use:<br/>
	/// Have a class extend this and call <see cref="Start"/> and supply your instance. This will store your instance in memory for later use. See <see cref="Instance{T}"/>.<br/>
	/// Use <see cref="Tick"/> for code that needs to run periodically.<br/>
	/// Use <see cref="AddRenderers"/> to add renderers that you wish to use.<br/>
	/// Use <see cref="Init"/> to run initialization code.<br/>
	/// Read the source code for more methods/variables you should use.
	/// </summary>
	[PublicAPI]
	public abstract class GameEngine {
		private static GameEngine instance = default!;

		/// <typeparam name="T"> The <see cref="Type"/> of your program. </typeparam>
		/// <returns> The instance of your program. </returns>
		protected static T Instance<T>() where T : GameEngine => (T)instance;

		private static Lazy<Type> InstanceType { get; } = new(() => instance.GetType());
		/// <summary> A <see cref="Lazy{T}"/> access to the instance's assembly. </summary>
		public static Lazy<Assembly> InstanceAssembly { get; } = new(() => Assembly.GetAssembly(InstanceType.Value) ?? throw new Exception("Assembly cannot be found."));
		/// <summary> A <see cref="Lazy{T}"/> access to USharpEngine's assembly. </summary>
		public static Lazy<Assembly> USharpEngineAssembly { get; } = new(() => Assembly.GetAssembly(typeof(GameEngine)) ?? throw new Exception("Assembly cannot be found."));

		/// <summary> Called after fullscreen mode has been toggled. </summary>
		protected event Action<WindowState>? FullscreenToggleEvent;
		/// <summary> Called when the program is ready for shader creation. The return value is all of your program's shaders. </summary>
		protected event Func<HashSet<IUnboundShader>>? ShaderCreationEvent;
		/// <summary> Called when the program is ready for font creation. The return value is all of your program's fonts. </summary>
		protected event Func<HashSet<Font>>? FontCreationEvent;
		/// <summary> Called when the program is ready for texture creation. The return value is all of your program's textures. </summary>
		protected event Func<HashSet<Texture>>? TextureCreationEvent;
		/// <summary> Called when the program is ready for screen creation. The return value is all of your program's screens. </summary>
		protected event Func<HashSet<Screen>>? ScreenCreationEvent;

		/// <summary> Called after the OpenGL window has been created. </summary>
		protected event Action<EngineWindow>? WindowCreationEvent;
		/// <summary> Called before <see cref="SetupGL"/> but after OpenGL has been initialized. </summary>
		protected event Action? OnSetupLoadingScreenEvent;
		/// <summary> Called before the program is closed by the user. </summary>
		protected event Action? OnClosingEvent;
		/// <summary> Called after fonts have finished being setup. </summary>
		protected event Action? OnFontsFinishedEvent;
		/// <summary> Called after textures have finished being setup. </summary>
		protected event Action? OnTexturesFinishedEvent;
		/// <summary> Called after shaders have finished being setup. </summary>
		protected event Action? OnShadersFinishedEvent;
		/// <summary> Called after screens have finished being setup. </summary>
		protected event Action? OnScreensFinishedEvent;
		/// <summary> Called after renderers have finished being setup. </summary>
		protected event Action? OnRenderersFinishedEvent;
		/// <summary> Called after all setup has finished and the program is ready. </summary>
		protected event Action? OnSetupFinishedEvent;

		/// <summary> Called when a key has been pressed. </summary>
		protected internal event Action<KeyboardKeyEventArgs>? OnKeyPressEvent;
		/// <summary> Called when a key has been released. </summary>
		protected internal event Action<KeyboardKeyEventArgs>? OnKeyReleaseEvent;
		/// <summary> Called when the mouse has been moved. </summary>
		protected internal event Action<MouseMoveEventArgs>? OnMouseMoveEvent;
		/// <summary> Called when a mouse button has been pressed. </summary>
		protected internal event Action<MouseButtonEventArgs>? OnMousePressEvent;
		/// <summary> Called when a mouse button has been released. </summary>
		protected internal event Action<MouseButtonEventArgs>? OnMouseReleaseEvent;
		/// <summary> Called when the scroll wheel has been moved. </summary>
		protected internal event Action<MouseWheelEventArgs>? OnMouseScrollEvent;
		/// <summary> Called when text input has been received. </summary>
		protected internal event Action<TextInputEventArgs>? OnTextInputEvent;
		/// <summary> Called when the window is resized. </summary>
		protected internal event Action<ResizeEventArgs>? OnWindowResizeEvent;

		private static GameWindow Window { get; set; } = default!;
		/// <summary> The current load state of the program. <seealso cref="LoadState"/> </summary>
		public static LoadState CurrentLoadState { get; internal set; } = LoadState.NotStarted;
		/// <summary> Whether or not the program is in Debug mode. </summary>
		public static bool IsDebug { get; protected set; }
		/// <summary> Whether or not a close has been requested. </summary>
		public static bool CloseRequested { get; internal set; } // I don't like this but GameWindow#IsExiting doesn't seem to work sometimes
		/// <summary> The current Screen <seealso cref="Screen"/> </summary>
		public static Screen? CurrentScreen { get; set; }

		internal static uint RawFPS, RawTPS;
		internal static double RawFrameFrequency, RawTickFrequency;

		/// <summary> The current FPS. </summary>
		public static uint FPS => RawFPS;
		/// <summary> The Current TPS. </summary>
		public static uint TPS => RawTPS;
		/// <summary> The amount of time since the last frame draw in milliseconds. </summary>
		public static double FrameFrequency => RawFrameFrequency;
		/// <summary> The amount of time since the last tick in milliseconds. </summary>
		public static double TickFrequency => RawTickFrequency;

		private List<IRenderer> Renderers { get; } = new();
		private HashSet<Screen> Screens { get; } = new();

		/// <summary> The original title given in the constructor. <seealso cref="GameEngine(string, ushort, ushort, ushort, ushort, LogLevel, bool)"/> </summary>
		public string OriginalTitle { get; }
		/// <summary> Whether or not when the screen checks UI collision, if the mouse click should be canceled if UI is hit. Default behavior is to cancel </summary>
		protected internal bool ShouldScreenCheckCancelMouseEvent { get; set; } = true;
		/// <summary> The max amount of log files to keep on disk. </summary>
		protected ushort MaxAmountOfLogs { private get; set; } = 5;

		/// <summary> The mouse's current X coordinate. </summary>
		public static ushort MouseX { get; set; }
		/// <summary> The mouse's current Y coordinate. </summary>
		public static ushort MouseY { get; set; }

		private string title;
		private ushort minWidth;
		private ushort minHeight;
		private ushort maxWidth;
		private ushort maxHeight;

		/// <summary> The program's current title. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public string Title {
			get => title;
			protected set {
				title = value;
				Window.Title = title;
			}
		}

		/// <summary> The program's current minimum window width. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MinWidth {
			get => minWidth;
			protected set {
				minWidth = value;
				Window.MinimumSize = new(minWidth, MinHeight);
			}
		}

		/// <summary> The program's current minimum window height. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MinHeight {
			get => minHeight;
			protected set {
				minHeight = value;
				Window.MinimumSize = new(MinWidth, minHeight);
			}
		}

		/// <summary> The program's current maximum window width. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MaxWidth {
			get => maxWidth;
			protected set {
				maxWidth = value;
				Window.MaximumSize = new(maxWidth, MaxHeight);
			}
		}

		/// <summary> The program's current maximum window height. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MaxHeight {
			get => maxHeight;
			protected set {
				maxHeight = value;
				Window.MaximumSize = new(MaxWidth, maxHeight);
			}
		}

		/// <summary> The program's current window width </summary>
		public static ushort Width => (ushort)Window.Size.X;
		/// <summary> The program's current window height </summary>
		public static ushort Height => (ushort)Window.Size.Y;

		protected GameEngine(string title, ushort minWidth, ushort minHeight, ushort maxWidth, ushort maxHeight, LogLevel logLevel = LogLevel.More, bool isDebug = false) {
			OriginalTitle = title;
			this.title = title;
			this.minWidth = minWidth;
			this.minHeight = minHeight;
			this.maxWidth = maxWidth;
			this.maxHeight = maxHeight;
			IsDebug = isDebug;

			Thread.CurrentThread.Name = "Main";
			Logger.LogLevel = logLevel;
			Logger.SetupDefaultLogFolder(5, $"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");

			ShaderCreationEvent += () => DefaultShaders.AllShaders;

			OnMouseMoveEvent += e => {
				MouseX = (ushort)MathH.Floor(e.X);
				MouseY = (ushort)MathH.Floor(e.Y);
				CurrentScreen?.CheckForHover(MouseX, MouseY);
			};
		}

		protected GameEngine(string title, ushort minWidth, ushort minHeight, LogLevel logLevel = LogLevel.More, bool isDebug = false) : this(title, minWidth, minHeight, 0, 0, logLevel, isDebug) { }
		protected GameEngine(string title, LogLevel logLevel = LogLevel.More, bool isDebug = false) : this(title, 856, 482, 0, 0, logLevel, isDebug) { }

		/// <summary> This method will start all the behind the scenes logic. </summary>
		/// <remarks> This should be called only once at the start of your main method. Example below. </remarks>
		/// <example> <code> private static void Main() => Start(new Program()); </code> </example>
		/// <param name="instance"> The instance of your program. This will be stored for your later use automatically. See <see cref="Instance{T}()"/> </param>
		public static void Start(GameEngine instance) {
			CurrentLoadState = LoadState.PreInit;
			using (Window = (GameEngine.instance = instance).ProvideWindow()) {
				CurrentLoadState = LoadState.Init;

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
		internal void InvokeOnTextInputEvent(TextInputEventArgs e) => OnTextInputEvent?.Invoke(e);
		internal void InvokeOnWindowResizeEvent(ResizeEventArgs e) => OnWindowResizeEvent?.Invoke(e);

		/// <summary> Called before OpenGL has been initialized but after the constructor. </summary>
		protected virtual void Init() { }

		internal static void CreateGL() {
			Logger.Info($"Setting up OpenGL! Running OpenGL version: {OpenGL4.GetString(StringName.Version)}");
			OpenGL4.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			OpenGL4.Enable(EnableCap.Blend);
			OpenGL4.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();
		}

		internal void SetupGL() {
			HashSet<IUnboundShader> shaders = new();
			HashSet<Font> fonts = new();
			HashSet<Texture> textures = new();
			HashSet<Texture> fontTextures = new();

			if (ShaderCreationEvent != null) {
				foreach (Delegate d in ShaderCreationEvent.GetInvocationList()) { shaders.UnionWith((HashSet<IUnboundShader>)(d.DynamicInvoke() ?? new HashSet<IUnboundShader>())); }
			}

			if (FontCreationEvent != null) {
				foreach (Delegate d in FontCreationEvent.GetInvocationList()) { fonts.UnionWith((HashSet<Font>)(d.DynamicInvoke() ?? new HashSet<Font>())); }
			}

			if (TextureCreationEvent != null) {
				foreach (Delegate d in TextureCreationEvent.GetInvocationList()) { textures.UnionWith((HashSet<Texture>)(d.DynamicInvoke() ?? new HashSet<Texture>())); }
			}

			void Fonts() =>
					fonts.ForEach(f => {
						if (f.Setup() is { } texture) { fontTextures.Add(texture); }
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

			if (fontTextures.Count != 0) {
				GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
				Logger.Debug($"Setting up {fontTextures.Count} font textures took {TimeH.Time(() => fontTextures.ForEach(t => t.SetupGL())).Milliseconds}ms");
				GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
			}

			OnTexturesFinishedEvent?.Invoke();

			if (shaders.Count != 0) { Logger.Debug($"Setting up {shaders.Count} shaders took {TimeH.Time(Shaders).Milliseconds}ms"); }
			OnShadersFinishedEvent?.Invoke();

			if (Screens.Count != 0) { Logger.Debug($"Setting up {Screens.Count} screens took {TimeH.Time(() => Screens.ForEach(r => r.SetupGL())).Milliseconds}ms"); }
			OnScreensFinishedEvent?.Invoke();

			AddRenderers(Renderers);
			if (Renderers.Count != 0) { Logger.Debug($"Setting up {Renderers.Count} renderers took {TimeH.Time(() => Renderers.ForEach(r => r.SetupGL())).Milliseconds}ms"); }
			OnRenderersFinishedEvent?.Invoke();
		}

		/// <summary> Called 60 times a second. </summary>
		/// <param name="time"> The time since the last tick. </param>
		public virtual void Tick(double time) => CurrentScreen?.Tick(time);

		/// <summary> Called every time a frame is requested. </summary>
		/// <param name="time"> The time since the last frame was drawn. </param>
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

		/// <param name="renderers"> The list to append renderers too. </param>
		protected abstract void AddRenderers(List<IRenderer> renderers);

		/// <summary> Toggles fullscreen. </summary>
		public void ToggleFullscreen() {
			Window.WindowState = Window.WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
			FullscreenToggleEvent?.Invoke(Window.WindowState);
		}

		/// <inheritdoc cref="OpenTK.Windowing.Desktop.NativeWindow.IsKeyDown"/>
		public static bool IsKeyDown(Keys key) => Window.IsKeyDown(key);

		/// <summary> Call some code while in a specific load state. <br/> You should probably avoid calling this method since it can be dangerous. <br/> You have been warned. <seealso cref="LoadState"/> </summary>
		public static void CallWhileInLoadState(LoadState loadState, Action todo) {
			LoadState old = CurrentLoadState;
			CurrentLoadState = loadState;
			todo();
			CurrentLoadState = old;
		}

		/// <summary> Calls code on the Main <see cref="Thread"/>. Used for MultiThreaded projects. </summary>
		/// <param name="toCall"></param>
		public static void CallOnMainThread(Action toCall) => Window.CallOnMainThreadQueue.Enqueue(toCall);

		/// <summary> Used for custom windows. Do not override this unless you know what you are doing. <br/>
		/// In the event the default window lacks required functionally, you can extend <see cref="GameWindow"/> and provide a new class instance here.
		/// </summary>
		/// <returns> A new instance of a <see cref="GameWindow"/> class for use. </returns>
		protected virtual GameWindow ProvideWindow() => new EngineWindow(this);

		public enum LoadState : byte {
			NotStarted = 0,
			PreInit,
			Init,
			CreateGL,
			SetupGL,
			Done,
		}
	}
}