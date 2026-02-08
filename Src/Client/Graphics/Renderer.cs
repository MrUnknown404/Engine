using NLog;

namespace Engine3.Client.Graphics {
	public abstract class Renderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public bool CanRender { get; set; } = true;
		public bool ShouldDestroy { get; protected set; }

		public bool WasDestroyed { get; private set; }

		internal Renderer() { }

		public abstract void Setup(); // TODO have engine call this?
		protected internal abstract void Render(float delta);

		public abstract bool IsSameWindow(Window window);

		internal void Destroy() {
			if (WasDestroyed) {
				Logger.Warn($"{GetType().Name} was already destroyed");
				return;
			}

			PrepareCleanup();
			CleanupImGui();
			Cleanup();

			WasDestroyed = true;
		}

		protected abstract void PrepareCleanup();
		protected abstract void Cleanup();

		internal abstract void CleanupImGui();
	}

	public abstract class Renderer<TWindow, TBackend, TImGui> : Renderer where TWindow : Window where TBackend : EngineGraphicsBackend where TImGui : ImGuiBackend {
		protected TBackend GraphicsBackend { get; }
		protected TWindow Window { get; }
		protected TImGui? ImGuiBackend { get; }

		protected Renderer(TBackend graphicsBackend, TWindow window, TImGui? imGuiBackend) {
			GraphicsBackend = graphicsBackend;
			Window = window;
			ImGuiBackend = imGuiBackend;
		}

		public override bool IsSameWindow(Window window) => Window == window;

		internal override void CleanupImGui() => ImGuiBackend?.Cleanup();
	}
}