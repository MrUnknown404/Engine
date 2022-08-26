using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using USharpLibs.Common.Utils;
using USharpLibs.Common.Utils.Extensions;
using USharpLibs.Engine.Client;
using USharpLibs.Engine.Client.Font;
using USharpLibs.Engine.Client.GL;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine {
	public abstract class ClientBase {
		private static ClientBase instance = default!;
		public static T Instance<T>() where T : ClientBase => (T)instance;
		public static Lazy<Type> InstanceType { get; } = new(() => instance.GetType());
		public static Lazy<Assembly> InstanceAssembly { get; } = new(() => Assembly.GetAssembly(InstanceType.Value) ?? throw new Exception("Assembly cannot be found."));

		protected internal event Action<EngineWindow>? WindowCreationEvent;
		protected internal event Func<HashSet<IUnboundShader>> ShaderCreationEvent;
		protected internal event Func<HashSet<DynamicFont>> FontCreationEvent;
		protected internal event Func<HashSet<RawTexture>> TextureCreationEvent;
		protected internal event Action<WindowState>? FullscreenToggleEvent;

		public static EngineWindow Window { get; private set; } = default!;
		public static Logger Logger { get; private set; } = default!;
		public static LoadState LoadState { get; protected internal set; }
		public static bool IsDebug { get; private set; }
		public static bool CloseRequested { get; private set; } // I don't like this but GameWindow#IsExiting doesn't seem to work sometimes

		[SuppressMessage("IDE", "SA1401")]
		internal static uint RawFPS, RawTPS;
		[SuppressMessage("IDE", "SA1401")]
		internal static double RawFrameFrequency, RawTickFrequency;

		public static uint FPS { get => RawFPS; protected set => RawFPS = value; }
		public static uint TPS { get => RawTPS; protected set => RawTPS = value; }
		public static double FrameFrequency { get => RawFrameFrequency; protected set => RawFrameFrequency = value; }
		public static double TickFrequency { get => RawTickFrequency; protected set => RawTickFrequency = value; }

		public string Title { get; protected set; }
		public ushort MinWidth { get; protected set; }
		public ushort MinHeight { get; protected set; }
		private readonly List<IRenderer> renderers = new();
		private readonly HashSet<IUnboundShader> shaders = new();
		private readonly HashSet<DynamicFont> fonts = new();
		private readonly HashSet<RawTexture> textures = new();

		protected byte MaxAmountOfLogs { private get; set; } = 5;

		protected ClientBase(string loggerName, string title, ushort minWidth, ushort minHeight, Func<HashSet<IUnboundShader>> shaderCreationEvent, Func<HashSet<DynamicFont>> fontCreationEvent, Func<HashSet<RawTexture>> textureCreationEvent, bool isDebug = false) {
			Title = title;
			MinWidth = minWidth;
			MinHeight = minHeight;
			IsDebug = isDebug;
			ShaderCreationEvent += shaderCreationEvent;
			FontCreationEvent += fontCreationEvent;
			TextureCreationEvent += textureCreationEvent;

			const string DateFormat = "MM-dd-yyyy HH-mm-ss-fff";

			Logger = Logger.More(loggerName);
			LoggerWriter newOut = new(Console.Out, new FileStream($"Logs\\{DateTime.Now.ToString(DateFormat)}.log", FileMode.Create));
			Console.SetOut(newOut);
			Console.SetError(newOut);

			AppDomain.CurrentDomain.UnhandledException += (obj, args) => Logger.PrintException((Exception)args.ExceptionObject);

			Logger.WriteLine($"Starting {title}! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");
			Logger.WriteLine($"Logs -> {Directory.CreateDirectory("Logs").FullName}");

			List<DateTime> dates = new();
			foreach (string f in Directory.GetFiles("Logs")) {
				if (f.EndsWith(".log") && f.Length == 32 && DateTime.TryParseExact(f[5..^4], DateFormat, null, System.Globalization.DateTimeStyles.None, out DateTime time)) {
					dates.Add(time);
				}
			}

			if (dates.Count > MaxAmountOfLogs) {
				dates.Sort(DateTime.Compare);

				Logger.WriteLine($"Found too many log files. Deleting the oldest.");
				while (dates.Count > MaxAmountOfLogs) {
					File.Delete($"Logs\\{dates[0].ToString(DateFormat)}.log");
					dates.RemoveAt(0);
				}
			}
		}

		protected ClientBase(string source, string title, Func<HashSet<IUnboundShader>> shaderCreationEvent, Func<HashSet<DynamicFont>> fontCreationEvent, Func<HashSet<RawTexture>> textureCreationEvent, bool isDebug = false) : this(source, title, 856, 482, shaderCreationEvent, fontCreationEvent, textureCreationEvent, isDebug) { }

		public static void Start(ClientBase instance) {
			using (Window = new(ClientBase.instance = instance, instance.MinWidth, instance.MinHeight)) {
				instance.OnWindowCreation(Window);
				LoadState = LoadState.Init;
				Logger.WriteLine($"Running Init took {TimeH.Time(() => instance.Init()).Milliseconds}ms");
				Window.Run();
			}

			Logger.WriteLine("Goodbye!");
		}

		internal void OnWindowCreation(EngineWindow window) => WindowCreationEvent?.Invoke(window);
		internal void OnFullscreenToggle(WindowState state) => FullscreenToggleEvent?.Invoke(state);

		protected virtual void Init() { }
		protected internal virtual void OnSetupFinished() { }

		protected internal virtual void SetupGL() {
			Logger.WriteLine($"Setting up OpenGL! Running OpenGL version: {OpenGL4.GetString(StringName.Version)}");
			OpenGL4.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			OpenGL4.Enable(EnableCap.Blend);
			OpenGL4.Enable(EnableCap.DepthTest);
			OpenGL4.Enable(EnableCap.CullFace);
			OpenGL4.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			shaders.UnionWith(ShaderCreationEvent());
			fonts.UnionWith(FontCreationEvent());
			textures.UnionWith(TextureCreationEvent());

			AddRenderers(renderers);

			void Fonts() => fonts.ForEach(f => {
				f.SetupGL();
				textures.Add(f.Texture);
			});

			void Shaders() => shaders.ForEach(s => {
				s.SetupGL();
				Window.Resize += s.OnResize;
			});

			if (shaders.Count != 0) { Logger.WriteLine($"Setting up {shaders.Count} shaders took {TimeH.Time(Shaders).Milliseconds}ms"); }
			if (fonts.Count != 0) { Logger.WriteLine($"Setting up {fonts.Count} fonts took {TimeH.Time(Fonts).Milliseconds}ms"); }
			if (textures.Count != 0) { Logger.WriteLine($"Setting up {textures.Count} textures took {TimeH.Time(() => textures.ForEach(t => t.SetupGL())).Milliseconds}ms"); }
			if (renderers.Count != 0) { Logger.WriteLine($"Setting up {renderers.Count} renderers took {TimeH.Time(() => renderers.ForEach(r => r.SetupGL())).Milliseconds}ms"); }
		}

		public virtual void Tick(double time) { }
		public virtual void Render(double time) => renderers.ForEach(r => r.Render(time));

		protected internal virtual void OnResize(ResizeEventArgs e, Vector2i size) => OpenGL4.Viewport(0, 0, size.X, size.Y);

		protected internal virtual void OnKeyPress(KeyboardKeyEventArgs e) { }
		protected internal virtual void OnKeyRelease(KeyboardKeyEventArgs e) { }
		protected internal virtual void OnMouseMove(MouseMoveEventArgs e) { }
		protected internal virtual void OnMousePress(MouseButtonEventArgs e) { }
		protected internal virtual void OnMouseRelease(MouseButtonEventArgs e) { }
		protected internal virtual void OnMouseScroll(MouseWheelEventArgs e) { }
		protected internal virtual void OnClosing(CancelEventArgs e) { CloseRequested = true; }

		protected virtual void AddRenderers(List<IRenderer> renderers) { }
	}

	public enum LoadState {
		NotStarted = 0,
		Init,
		GL,
		Done,
	}
}