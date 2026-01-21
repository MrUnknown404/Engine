using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace Engine3.Graphics.OpenGL {
	public class GlWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public OpenGLContextHandle GLContextHandle { get; }
		public VertexArrayHandle EmptyVao { get; }

		public ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

		private GlWindow(WindowHandle windowHandle, OpenGLContextHandle glContextHandle, VertexArrayHandle emptyVao) : base(windowHandle) {
			GLContextHandle = glContextHandle;
			EmptyVao = emptyVao;
		}

		internal static GlWindow MakeGlWindow(WindowHandle windowHandle) {
			Logger.Debug("Creating and setting OpenGL context...");
			OpenGLContextHandle openGLContextHandle = Toolkit.OpenGL.CreateFromWindow(windowHandle);
			Toolkit.OpenGL.SetCurrentContext(openGLContextHandle);
			GLLoader.LoadBindings(Toolkit.OpenGL.GetBindingsContext(openGLContextHandle)); // do i call this per window?
			Logger.Debug($"- Version: {GL.GetString(StringName.Version)}");

#if DEBUG
			Logger.Debug("- Debug callbacks enabled");
			GLH.CreateDebugMessageCallback(); // must be called after GLLoader.LoadBindings. otherwise i'd move this into Engine3
#endif

			VertexArrayHandle emptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray((int)emptyVao); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has Handle: {emptyVao.Handle}");

			GlWindow window = new(windowHandle, openGLContextHandle, emptyVao);

			GL.ClearColor(window.ClearColor);
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			return window;
		}

		protected override void CleanupGraphics() { }
	}
}