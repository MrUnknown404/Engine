using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace Engine3.Graphics.OpenGL {
	public abstract class GlRenderer : Renderer {
		protected readonly GlWindow Window;

		protected GlRenderer(GlWindow window) => Window = window;

		protected internal override void DrawFrame(float delta) {
			Toolkit.OpenGL.SetCurrentContext(Window.GLContextHandle);
			GL.Clear(Window.ClearBufferMask);
			Draw(delta);
			Toolkit.OpenGL.SwapBuffers(Window.GLContextHandle);
		}

		protected abstract void Draw(float delta);
	}
}