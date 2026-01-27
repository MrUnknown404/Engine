using Engine3.Utility;
using OpenTK.Platform;

namespace Engine3.Client.Graphics {
	public abstract class EngineGraphicsBackend : IDestroyable {
		public GraphicsBackend GraphicsBackend { get; }
		public GraphicsApiHints? GraphicsApiHints { get; }

		public bool WasDestroyed { get; private set; }

		protected EngineGraphicsBackend(GraphicsBackend graphicsBackend, GraphicsApiHints? graphicsApiHints) {
			GraphicsBackend = graphicsBackend;
			GraphicsApiHints = graphicsApiHints;
		}

		protected internal abstract void Setup(GameClient gameClient);
		protected internal abstract void Cleanup();

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Cleanup();

			WasDestroyed = true;
		}
	}
}