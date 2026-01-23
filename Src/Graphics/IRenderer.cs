namespace Engine3.Graphics {
	public interface IRenderer : IDestroyable {
		public Window BoxedWindow { get; } // TODO see if i can remove this or add generic

		public ulong FrameCount { get; }
		public bool CanRender { get; set; }
		public bool ShouldDestroy { get; set; }

		public void Setup();
		public void Render(float delta);
	}
}