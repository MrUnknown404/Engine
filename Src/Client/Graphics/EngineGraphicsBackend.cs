using OpenTK.Platform;

namespace Engine3.Client.Graphics {
	public abstract class EngineGraphicsBackend {
		public GraphicsBackend GraphicsBackend { get; }
		public GraphicsApiHints? GraphicsApiHints { get; }

		protected EngineGraphicsBackend(GraphicsBackend graphicsBackend, GraphicsApiHints? graphicsApiHints) {
			GraphicsBackend = graphicsBackend;
			GraphicsApiHints = graphicsApiHints;
		}

		protected internal abstract void Setup(GameClient gameClient);
		protected internal abstract void Cleanup();
	}
}