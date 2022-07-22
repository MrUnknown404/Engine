using System.Diagnostics.CodeAnalysis;
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

		protected internal event Action<EngineWindow>? WindowCreationEvent;
		protected internal event Func<HashSet<IUnboundShader>> ShaderCreationEvent;
		protected internal event Func<HashSet<DynamicFont>> FontCreationEvent;
		protected internal event Func<HashSet<RawTexture>> TextureCreationEvent;

		public static EngineWindow Window { get; private set; } = default!;
		public static Logger Logger { get; private set; } = default!;
		public static string Source { get; private set; } = string.Empty;
		public static LoadState LoadState { get; protected internal set; }
		public static bool IsDebug { get; private set; }

		[SuppressMessage("IDE", "SA1401")]
		internal static uint RawFPS, RawTPS;
		[SuppressMessage("IDE", "SA1401")]
		internal static double RawframeFrequency, RawTickFrequency;

		public static uint FPS { get => RawFPS; protected set => RawFPS = value; }
		public static uint TPS { get => RawTPS; protected set => RawTPS = value; }
		public static double FrameFrequency { get => RawframeFrequency; protected set => RawframeFrequency = value; }
		public static double TickFrequency { get => RawTickFrequency; protected set => RawTickFrequency = value; }

		public string Title { get; protected set; } = string.Empty;
		private readonly List<IRenderer> renderers = new();
		private readonly HashSet<IUnboundShader> shaders = new();
		private readonly HashSet<DynamicFont> fonts = new();
		private readonly HashSet<RawTexture> textures = new();

		protected ClientBase(string source, string title, Func<HashSet<IUnboundShader>> shaderCreationEvent, Func<HashSet<DynamicFont>> fontCreationEvent, Func<HashSet<RawTexture>> textureCreationEvent, bool isDebug = false) {
			Source = source;
			Title = title;
			IsDebug = isDebug;
			ShaderCreationEvent += shaderCreationEvent;
			FontCreationEvent += fontCreationEvent;
			TextureCreationEvent += textureCreationEvent;

			Logger = Logger.More(Source);
		}

		public static void Start(ClientBase instance) {
			Logger.WriteLine($"Starting {instance.Title}! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");

			using (Window = new(ClientBase.instance = instance)) {
				LoadState = LoadState.Init;
				Logger.WriteLine($"Running Init took {TimeH.Time(() => instance.Init()).Milliseconds}ms");
				Window.Run();
			}

			Logger.WriteLine("Goodbye!");
		}

		internal void OnWindowCreation(EngineWindow window) => WindowCreationEvent?.Invoke(window);

		protected virtual void Init() { }

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

			void Fonts() {
				fonts.ForEach(f => {
					f.SetupGL();
					textures.Add(f.Texture);
				});
			}

			Logger.WriteLine($"Setting up {shaders.Count} shaders took {TimeH.Time(() => shaders.ForEach(s => s.SetupGL())).Milliseconds}ms");
			Logger.WriteLine($"Setting up {fonts.Count} fonts took {TimeH.Time(Fonts).Milliseconds}ms");
			Logger.WriteLine($"Setting up {textures.Count} textures took {TimeH.Time(() => textures.ForEach(t => t.SetupGL())).Milliseconds}ms");
			Logger.WriteLine($"Setting up {renderers.Count} renderers took {TimeH.Time(() => renderers.ForEach(r => r.SetupGL())).Milliseconds}ms");
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

		protected virtual void AddRenderers(List<IRenderer> renderers) { }
	}

	public enum LoadState {
		NotStarted = 0,
		Init,
		GL,
		Done,
	}
}