using Engine3.Utility;

namespace Engine3.Client.Graphics {
	public abstract class Renderer : IDestroyable {
		public ulong FrameCount { get; protected set; }
		public bool CanRender { get; set; } = true;
		public bool ShouldDestroy { get; protected set; }

		public bool WasDestroyed { get; protected set; }

		internal Renderer() { }

		public abstract void Setup();
		protected internal abstract void Render(float delta);

		protected abstract void Cleanup();

		public abstract bool IsSameWindow(Window window);

		/// <summary>
		/// Note: Because Cleanup() needs to wait until the end of the frame this method won't destroy the object.<br/>
		/// Instead, it'll set <see cref="ShouldDestroy"/> to <c>true</c> and the engine will destroy this on the beginning of the next frame.
		/// </summary>
		public abstract void Destroy();

		internal abstract void ActuallyDestroy();
	}

	public abstract class Renderer<TWindow, TBackend> : Renderer where TWindow : Window where TBackend : EngineGraphicsBackend {
		protected TBackend GraphicsBackend { get; }
		protected TWindow Window { get; }

		protected Renderer(TBackend graphicsBackend, TWindow window) {
			GraphicsBackend = graphicsBackend;
			Window = window;
		}
	}
}