using NLog;
using OpenTK.Platform;

namespace Engine3.Client.Graphics {
	public abstract class EngineGraphicsBackend {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
			if (WasDestroyed) {
				Logger.Warn($"Tried to destroy a {nameof(Window)} that was already destroyed");
				return;
			}

			Cleanup();

			WasDestroyed = true;
		}
	}
}