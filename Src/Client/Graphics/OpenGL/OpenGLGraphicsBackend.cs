using OpenTK.Platform;

namespace Engine3.Client.Graphics.OpenGL {
	public class OpenGLGraphicsBackend : EngineGraphicsBackend {
		public OpenGLGraphicsBackendSettings Settings { get; init; } = new();

		public OpenGLGraphicsBackend(OpenGLGraphicsApiHints graphicsApiHints) : base(GraphicsBackend.OpenGL, graphicsApiHints) { }

		protected internal override void Setup(GameClient gameClient) => Settings.Print();
		protected internal override void Cleanup() { }
	}
}