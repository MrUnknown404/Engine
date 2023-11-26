using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.IO;
using USharpLibs.Common.Math;
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
	/// Use <see cref="OnSetupEvent"/> to run initialization code.<br/>
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

		/// <summary> Called before <see cref="OnInitEvent"/>. </summary>
		protected event Action? OnPreInitEvent;
		/// <summary> Called before <see cref="OnSetupGLEvent"/> and after <see cref="OnPreInitEvent"/> </summary>
		protected event Action? OnInitEvent;
		/// <summary> Called before <see cref="OnSetupGLObjectsEvent"/> and after <see cref="OnInitEvent"/> </summary>
		protected event Action? OnSetupGLEvent;
		/// <summary> Called before <see cref="OnSetupEngineEvent"/> and after <see cref="OnSetupGLEvent"/> </summary>
		private event Action? OnSetupGLObjectsEvent;
		/// <summary> Called before <see cref="OnSetupLoadingScreenEvent"/> and after <see cref="OnSetupGLObjectsEvent"/> </summary>
		private event Action? OnSetupEngineEvent;
		/// <summary> Called before <see cref="OnSetupEvent"/> and after <see cref="OnSetupEngineEvent"/> </summary>
		protected event Action? OnSetupLoadingScreenEvent;
		/// <summary> Called before <see cref="OnPostInitEvent"/> and after <see cref="OnSetupLoadingScreenEvent"/> </summary>
		protected event Action? OnSetupEvent;
		/// <summary> Called before <see cref="OnSetupFinishedEvent"/> and after <see cref="OnSetupEvent"/> </summary>
		protected event Action? OnPostInitEvent;
		/// <summary> Called after <see cref="OnPostInitEvent"/> </summary>
		protected event Action? OnSetupFinishedEvent;
		/// <summary> Called when a close is requested. Return true if you want to cancel the request. This is called before <see cref="OnUnloadEvent"/> </summary>
		protected event Func<bool>? OnClosingEvent;
		/// <summary> Called before the system exists. <see cref="OnClosingEvent"/> is called before this </summary>
		protected event Action? OnUnloadEvent;

		/// <summary> Called when the program is ready for shader creation. The return value is all of your program's shaders. </summary>
		protected event Func<HashSet<IUnboundShader>>? ShaderCreationEvent;
		/// <summary> Called when the program is ready for font creation. The return value is all of your program's fonts. </summary>
		protected event Func<HashSet<Font>>? FontCreationEvent;
		/// <summary> Called when the program is ready for texture creation. The return value is all of your program's textures. </summary>
		protected event Func<HashSet<Texture>>? TextureCreationEvent;
		/// <summary> Called when the program is ready for screen creation. The return value is all of your program's screens. </summary>
		protected event Func<HashSet<Screen>>? ScreenCreationEvent;
		/// <summary> Called when the program is ready for renderer creation. The return value is all of your program's renderers. </summary>
		protected event Func<List<IRenderer>>? RendererCreationEvent;

		/// <summary> Called after shaders have finished being setup. </summary>
		protected event Action? OnShadersFinishedEvent;
		/// <summary> Called after fonts have finished being setup. </summary>
		protected event Action? OnFontsFinishedEvent;
		/// <summary> Called after textures have finished being setup. </summary>
		protected event Action? OnTexturesFinishedEvent;
		/// <summary> Called after screens have finished being setup. </summary>
		protected event Action? OnScreensFinishedEvent;
		/// <summary> Called after renderers have finished being setup. </summary>
		protected event Action? OnRenderersFinishedEvent;

		/// <summary> Called after fullscreen mode has been toggled. </summary>
		protected event Action<WindowState>? FullscreenToggleEvent;
		/// <summary> Called when the window is resized. </summary>
		protected event Action<ResizeEventArgs>? OnWindowResizeEvent;

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

		private static GameWindow Window { get; set; } = default!;

		/// <summary> The current load state of the program. <seealso cref="LoadState"/> </summary>
		public static LoadState CurrentLoadState { get; internal set; } = LoadState.NotStarted;
		/// <summary> Whether or not the program is in Debug mode. </summary>
		public static bool IsDebug { get; protected set; }
		/// <summary> Whether or not OpenGL has been initialized </summary>
		public static bool OpenGLInitialized { get; protected set; }
		/// <summary> Whether or not a close has been requested. </summary>
		public static bool CloseRequested { get; internal set; } // I don't like this but GameWindow#IsExiting doesn't seem to work sometimes
		public static bool IsRunningSlowly => Window.IsRunningSlowly;

		/// <summary> The current Screen <seealso cref="Screen"/> </summary>
		public static Screen? CurrentScreen {
			get => currentScreen;
			set {
				currentScreen = value;
				currentScreen?.OnEnable();
			}
		}

		private static Screen? currentScreen;
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

		private HashSet<Screen> Screens { get; } = new();
		private HashSet<IRenderer> Renderers { get; } = new();

		/// <summary> The original title given in the constructor. <seealso cref="GameEngine(string, ushort, ushort, ushort, ushort, LogLevel, bool)"/> </summary>
		public string OriginalTitle { get; }
		/// <summary> Whether or not when the screen checks UI collision, if the mouse click should be canceled if UI is hit. Default behavior is to cancel </summary>
		protected internal bool ShouldScreenCheckCancelMouseEvent { get; set; } = true;
		/// <summary> The max amount of log files to keep on disk. </summary>
		protected ushort MaxAmountOfLogs { private get; set; } = 5;

		/// <summary> The mouse's current X coordinate. </summary>
		public static ushort MouseX { get; private set; }
		/// <summary> The mouse's current Y coordinate. </summary>
		public static ushort MouseY { get; private set; }

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
				Window.Title = Title;
			}
		}

		/// <summary> The program's current minimum window width. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MinWidth {
			get => minWidth;
			protected set {
				minWidth = value;
				Window.MinimumSize = new(MinWidth, MinHeight);
			}
		}

		/// <summary> The program's current minimum window height. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MinHeight {
			get => minHeight;
			protected set {
				minHeight = value;
				Window.MinimumSize = new(MinWidth, MinHeight);
			}
		}

		/// <summary> The program's current maximum window width. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MaxWidth {
			get => maxWidth;
			protected set {
				maxWidth = value;
				Window.MaximumSize = new(MaxWidth, MaxHeight);
			}
		}

		/// <summary> The program's current maximum window height. <remarks> Changes will be automatically reflected into OpenGL. </remarks> </summary>
		public ushort MaxHeight {
			get => maxHeight;
			protected set {
				maxHeight = value;
				Window.MaximumSize = new(MaxWidth, MaxHeight);
			}
		}

		/// <summary> The program's current window width </summary>
		public static ushort Width => (ushort)Window.ClientSize.X;
		/// <summary> The program's current window height </summary>
		public static ushort Height => (ushort)Window.ClientSize.Y;

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

			OnSetupGLEvent += OnSetupGL;
			OnSetupGLObjectsEvent += OnSetupGLObjects;
			OnSetupEngineEvent += OnSetupEngine;
		}

		protected GameEngine(string title, ushort minWidth, ushort minHeight, LogLevel logLevel = LogLevel.More, bool isDebug = false) : this(title, minWidth, minHeight, 0, 0, logLevel, isDebug) { }
		protected GameEngine(string title, LogLevel logLevel = LogLevel.More, bool isDebug = false) : this(title, 856, 482, 0, 0, logLevel, isDebug) { }

		/// <summary> This method will start all the behind the scenes logic. </summary>
		/// <remarks> This should be called only once at the start of your main method. Example below. </remarks>
		/// <example> <code> private static void Main() => Start(new Program()); </code> </example>
		/// <param name="instance"> The instance of your program. This will be stored for your later use automatically. See <see cref="Instance{T}()"/> </param>
		public static void Start(GameEngine instance) {
			GameEngine.instance = instance;

			CurrentLoadState = LoadState.PreInit;
			instance.OnPreInitEvent?.Invoke();

			using (Window = instance.ProvideWindow()) {
				CurrentLoadState = LoadState.Init;
				Logger.Debug($"Running Init took {TimeH.Time(() => instance.OnInitEvent?.Invoke()).Milliseconds}ms");
				Window.Run(instance);
			}

			Logger.Info("Goodbye!");
		}

		internal void InvokeOnSetupGLEvent() => OnSetupGLEvent?.Invoke();
		internal void InvokeOnSetupGLObjectsEvent() => OnSetupGLObjectsEvent?.Invoke();
		internal void InvokeOnSetupLoadingScreenEvent() => OnSetupLoadingScreenEvent?.Invoke();
		internal void InvokeOnSetupEngineEvent() => OnSetupEngineEvent?.Invoke();
		internal void InvokeOnSetupEvent() => OnSetupEvent?.Invoke();
		internal void InvokeOnPostInitEvent() => OnPostInitEvent?.Invoke();
		internal void InvokeOnSetupFinishedEvent() => OnSetupFinishedEvent?.Invoke();
		internal void InvokeOnUnloadEvent() => OnUnloadEvent?.Invoke();

		internal void InvokeOnWindowResizeEvent(ResizeEventArgs e) => OnWindowResizeEvent?.Invoke(e);
		internal bool InvokeOnClosingEvent() => OnClosingEvent != null && OnClosingEvent.GetInvocationList().Aggregate(false, (current, d) => current | (bool)(d.DynamicInvoke() ?? false));

		internal void InvokeOnKeyPressEvent(KeyboardKeyEventArgs e) => OnKeyPressEvent?.Invoke(e);
		internal void InvokeOnKeyReleaseEvent(KeyboardKeyEventArgs e) => OnKeyReleaseEvent?.Invoke(e);
		internal void InvokeOnMouseMoveEvent(MouseMoveEventArgs e) => OnMouseMoveEvent?.Invoke(e);
		internal void InvokeOnMousePressEvent(MouseButtonEventArgs e) => OnMousePressEvent?.Invoke(e);
		internal void InvokeOnMouseReleaseEvent(MouseButtonEventArgs e) => OnMouseReleaseEvent?.Invoke(e);
		internal void InvokeOnMouseScrollEvent(MouseWheelEventArgs e) => OnMouseScrollEvent?.Invoke(e);
		internal void InvokeOnTextInputEvent(TextInputEventArgs e) => OnTextInputEvent?.Invoke(e);

		private static void OnSetupGL() {
			Logger.Info("Setting up OpenGL!");
			Logger.Info($"- OpenGL version: {OpenGL4.GetString(StringName.Version)}");
			Logger.Info($"- GLFW Version: {GLFW.GetVersionString()}");

			OpenGL4.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			OpenGL4.Enable(EnableCap.Blend);
			OpenGL4.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();

			OpenGLInitialized = true;
		}

		private void OnSetupGLObjects() {
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

			// Shaders
			if (shaders.Count != 0) {
				Logger.Debug($"Setting up {shaders.Count} shaders took {TimeH.Time(() => {
					shaders.ForEach(s => {
						s.SetupGL();
						Window.Resize += s.OnResize;
					});
				}).Milliseconds}ms");
			}

			OnShadersFinishedEvent?.Invoke();

			// Fonts
			if (fonts.Count != 0) {
				Logger.Debug($"Setting up {fonts.Count} fonts took {TimeH.Time(() => {
					fonts.ForEach(f => {
						if (f.Setup() is { } texture) { fontTextures.Add(texture); }
					});
				}).Milliseconds}ms");
			}

			OnFontsFinishedEvent?.Invoke();

			// Textures
			if (textures.Count != 0) { Logger.Debug($"Setting up {textures.Count} textures took {TimeH.Time(() => textures.ForEach(t => t.SetupGL())).Milliseconds}ms"); }
			if (fontTextures.Count != 0) {
				GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
				Logger.Debug($"Setting up {fontTextures.Count} font textures took {TimeH.Time(() => fontTextures.ForEach(t => t.SetupGL())).Milliseconds}ms");
				GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
			}

			OnTexturesFinishedEvent?.Invoke();
		}

		private void OnSetupEngine() {
			//Screens
			if (ScreenCreationEvent != null) {
				foreach (Delegate d in ScreenCreationEvent.GetInvocationList()) { Screens.UnionWith((HashSet<Screen>)(d.DynamicInvoke() ?? new HashSet<Screen>())); }
			}

			if (Screens.Count != 0) { Logger.Debug($"Setting up {Screens.Count} screens took {TimeH.Time(() => Screens.ForEach(s => s.SetupGL())).Milliseconds}ms"); }
			OnScreensFinishedEvent?.Invoke();

			//Renderers
			if (RendererCreationEvent != null) {
				foreach (Delegate d in RendererCreationEvent.GetInvocationList()) { Renderers.UnionWith((List<IRenderer>)(d.DynamicInvoke() ?? new List<IRenderer>())); }
			}

			if (Renderers.Count != 0) { Logger.Debug($"Setting up {Renderers.Count} renderers took {TimeH.Time(() => Renderers.ForEach(s => s.SetupGL())).Milliseconds}ms"); }
			OnRenderersFinishedEvent?.Invoke();
		}

		/// <summary> Called 60 times a second. </summary>
		/// <param name="time"> The time since the last tick. </param>
		public virtual void Tick(double time) {
			Renderers.ForEach(r => r.Tick(time));
			CurrentScreen?.Tick(time);
		}

		/// <summary> Called every time a frame is requested. </summary>
		/// <param name="time"> The time since the last frame was drawn. </param>
		public virtual void Render(double time) {
			Renderers.ForEach(r => r.Render(time));
			CurrentScreen?.Render(time);
		}

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

		/// <summary> Toggles fullscreen. </summary>
		public void ToggleFullscreen() {
			Window.WindowState = Window.WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
			FullscreenToggleEvent?.Invoke(Window.WindowState);
		}

		public static void SetVSync(VSyncMode vsync) => Window.VSync = vsync;
		public static void SwapBuffers() => Window.SwapBuffers();
		public static void RequestClose() => Window.Close();

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
			SetupEngine,
			Setup,
			PostInit,
			Done,
		}

		protected internal virtual void SetupViewport(ResizeEventArgs e) => OpenGL4.Viewport(0, 0, e.Width, e.Height);
	}
}