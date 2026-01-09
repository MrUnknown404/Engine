using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace Engine3.Graphics.OpenGL {
	public class GlWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public OpenGLContextHandle GLContextHandle { get; }
		public VertexArrayHandle EmptyVao { get; }

		private GlWindow(WindowHandle windowHandle, OpenGLContextHandle glContextHandle, VertexArrayHandle emptyVao) : base(windowHandle) {
			GLContextHandle = glContextHandle;
			EmptyVao = emptyVao;
		}

		internal static GlWindow MakeGlWindow(WindowHandle windowHandle) {
			Logger.Debug("Creating and setting OpenGL context...");
			OpenGLContextHandle openGLContextHandle = Toolkit.OpenGL.CreateFromWindow(windowHandle);
			Toolkit.OpenGL.SetCurrentContext(openGLContextHandle);
			GLLoader.LoadBindings(Toolkit.OpenGL.GetBindingsContext(openGLContextHandle));
			Logger.Debug($"- Version: {GL.GetString(StringName.Version)}");

#if DEBUG
			Logger.Debug("- Debug callbacks enabled");
			GLH.CreateDebugMessageCallback(); // must be called after GLLoader.LoadBindings. otherwise i'd move this into Engine3
#endif

			GL.ClearColor(new(0.1f, 0.1f, 0.1f, 1));
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			VertexArrayHandle emptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray((int)emptyVao); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has Handle: {emptyVao.Handle}");

			return new(windowHandle, openGLContextHandle, emptyVao);
		}

		protected override void CleanupGraphics() {
			// TODO OpenGL doesn't seem to need to be cleaned up?
		}
	}
}