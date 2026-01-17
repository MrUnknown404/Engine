namespace Engine3.Graphics {
	public abstract class Renderer {
		public abstract bool IsWindowValid { get; }
		public bool WasDestroyed { get; private set; }

		public bool CanRender => IsWindowValid && !WasDestroyed;

		public abstract void Setup();
		protected internal abstract void DrawFrame(float delta);

		public void TryCleanup() {
			if (!WasDestroyed) {
				Cleanup();
				WasDestroyed = true;
			}
		}

		protected abstract void Cleanup();
	}
}