using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics.OpenGL {
	public abstract class GlRenderer : Renderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected VertexArrayHandle? EmptyVao { get; private set; }
		protected readonly GlWindow Window;

		protected GlRenderer(GlWindow window) => Window = window;

		public override void Setup() {
			Window.MakeContextCurrent();

			EmptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray(EmptyVao.Value.Handle); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has Handle: {EmptyVao.Value.Handle}");
		}

		protected override void DrawFrame(float delta) {
			if (!CanRender) { return; }

			Window.MakeContextCurrent();
			GL.ClearColor(Window.ClearColor);
			GL.Clear(Window.ClearBufferMask);

			if (Window.WasResized) {
				Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i framebufferSize);
				GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y);
				Window.WasResized = false;
			}

			Draw(delta);
			Toolkit.OpenGL.SwapBuffers(Window.GLContextHandle);
		}

		protected abstract void Draw(float delta);
	}
}