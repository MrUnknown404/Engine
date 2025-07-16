using Engine3.Graphics;
using JetBrains.Annotations;
using OpenTK.Platform;

namespace Engine3.Api.Graphics {
	[PublicAPI]
	public interface IRenderContext {
		public RenderSystem RenderSystem { get; }
		public void Setup(WindowHandle windowHandle);
		public void PrepareFrame();
		public void SwapBuffers();
	}
}