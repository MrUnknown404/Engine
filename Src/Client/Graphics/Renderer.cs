using NLog;

namespace Engine3.Client.Graphics {
	public abstract class Renderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public bool CanRender { get; set; } = true;
		public bool ShouldDestroy { get; protected set; }

		public bool WasDestroyed { get; private set; }

		internal Renderer() { }

		public abstract void Setup();
		protected internal abstract void Render(float delta);

		public abstract bool IsSameWindow(Window window);

		internal void Destroy() {
			if (WasDestroyed) {
				Logger.Warn($"{GetType().Name} was already destroyed");
				return;
			}

			PrepareCleanup();
			Cleanup();

			WasDestroyed = true;
		}

		protected abstract void PrepareCleanup();
		protected abstract void Cleanup();
	}

	public abstract class Renderer<TWindow, TBackend> : Renderer where TWindow : Window where TBackend : EngineGraphicsBackend {
		protected TBackend GraphicsBackend { get; }
		protected TWindow Window { get; }

		protected Renderer(TBackend graphicsBackend, TWindow window) {
			GraphicsBackend = graphicsBackend;
			Window = window;
		}

		public override bool IsSameWindow(Window window) => Window == window;
	}
}