namespace Engine3.Client.Graphics {
	public class SceneRenderer : IRenderer {
		protected Window Window { get; }

		public Scene.Scene? Scene { get; set; }

		public ulong FrameCount { get; private set; }
		public bool CanRender { get; set; }
		public bool ShouldDestroy { get; set; }
		public bool WasDestroyed { get; private set; }

		public SceneRenderer(Window window) => Window = window;

		public void Setup() { }

		public void Render(float delta) {
			if (Scene != null) {
				// TODO draw our objects. how the hell is this going to work
			}

			FrameCount++;
		}

		public bool IsSameWindow(Window window) => Window == window;

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			WasDestroyed = true;
		}
	}
}