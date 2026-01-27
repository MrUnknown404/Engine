using OpenTK.Platform;

namespace Engine3.Client.Graphics {
	public class OpenGLGraphicsBackend : EngineGraphicsBackend {
		public uint[] DisabledCallbackIds { get; init; } = Array.Empty<uint>();
		public int SwapInterval { get; init; }

		public OpenGLGraphicsBackend(OpenGLGraphicsApiHints graphicsApiHints) : base(GraphicsBackend.OpenGL, graphicsApiHints) { }
		protected internal override void Setup(GameClient gameClient) { }
		protected internal override void Cleanup() { }
	}
}