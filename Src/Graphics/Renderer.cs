namespace Engine3.Graphics {
	public abstract class Renderer {
		public ulong FrameCount { get; private set; }
		public bool WasDestroyed { get; private set; }

		public abstract bool IsWindowValid { get; }

		public bool CanRender => IsWindowValid && !WasDestroyed;

		public abstract void Setup();
		protected abstract void DrawFrame(float delta);

		internal void InternalDrawFrame(float delta) {
			DrawFrame(delta);
			FrameCount++;
		}

		public void TryDestroy() {
			if (!WasDestroyed) {
				Destroy();
				WasDestroyed = true;
			}
		}

		protected abstract void Destroy();
	}
}