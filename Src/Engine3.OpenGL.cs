using Engine3.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace Engine3 {
	public static partial class Engine3 {
		private static void GlRender(GameClient gameClient, float delta) {
			foreach (GlWindow window in Windows.OfType<GlWindow>().Where(static w => !w.WasDestroyed)) {
				if (window.ShouldClose) {
					window.DestroyWindow();
					continue;
				}

				Toolkit.OpenGL.SetCurrentContext(window.GLContextHandle);

				GL.Clear(window.ClearBufferMask);
				GlRender(delta);
				gameClient.GlRender(delta);
				Toolkit.OpenGL.SwapBuffers(window.GLContextHandle);
			}
		}

		private static void GlRender(float delta) { }
	}
}