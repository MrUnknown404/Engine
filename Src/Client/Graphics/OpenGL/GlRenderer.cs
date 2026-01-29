using Engine3.Utility;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.OpenGL {
	public abstract class GlRenderer : Renderer<GlWindow, OpenGLGraphicsBackend> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected VertexArrayHandle? EmptyVao { get; private set; }

		public ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;

		protected GlRenderer(OpenGLGraphicsBackend graphicsBackend, GlWindow window) : base(graphicsBackend, window) { }

		public override void Setup() {
			Window.MakeContextCurrent();

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.ClearColor(Window.ClearColor);

			Toolkit.OpenGL.SetSwapInterval(GraphicsBackend.SwapInterval);

			EmptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray(EmptyVao.Value.Handle); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has Handle: {EmptyVao.Value.Handle}");
		}

		protected internal override void Render(float delta) {
			Window.MakeContextCurrent();

			GL.ClearColor(Window.ClearColor);
			GL.Clear(ClearBufferMask);

			if (Window.WasResized) {
				Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i framebufferSize);
				GL.Viewport(0, 0, framebufferSize.X, framebufferSize.Y);
				Window.WasResized = false;
			}

			DrawFrame(delta);

			Toolkit.OpenGL.SwapBuffers(Window.GLContextHandle);

			FrameCount++;
		}

		protected abstract void DrawFrame(float delta);

		public override bool IsSameWindow(Window window) => Window == window;

		public override void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			if (!ShouldDestroy) {
				ShouldDestroy = true;
				return;
			}

			ActuallyDestroy();
		}

		internal override void ActuallyDestroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Window.MakeContextCurrent();
			Cleanup();

			WasDestroyed = true;
		}
	}
}