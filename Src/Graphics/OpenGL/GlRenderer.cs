using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics.OpenGL {
	public abstract class GlRenderer : IRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected GlWindow Window { get; }
		protected VertexArrayHandle? EmptyVao { get; private set; }

		public ulong FrameCount { get; private set; }
		public bool CanRender { get; set; } = true;
		public bool ShouldDestroy { get; set; }
		public bool WasDestroyed { get; private set; }

		protected GlRenderer(GlWindow window) => Window = window;

		public virtual void Setup() {
			Window.MakeContextCurrent();

			EmptyVao = new(GL.CreateVertexArray());
			GL.BindVertexArray(EmptyVao.Value.Handle); // Some hardware requires vao to be bound even if it's not in use
			Logger.Debug($"EmptyVao has Handle: {EmptyVao.Value.Handle}");
		}

		public virtual void Render(float delta) {
			Window.MakeContextCurrent();
			GL.ClearColor(Window.ClearColor);
			GL.Clear(Window.ClearBufferMask);

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

		protected abstract void Cleanup();

		public bool IsSameWindow(Window window) => Window == window;

		[Obsolete($"Warning. Do not call. Set {nameof(ShouldDestroy)}")]
		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Window.MakeContextCurrent();
			Cleanup();

			WasDestroyed = true;
		}
	}
}