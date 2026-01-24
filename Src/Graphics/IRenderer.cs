using Engine3.Utility;

namespace Engine3.Graphics {
	public interface IRenderer : IDestroyable {
		public ulong FrameCount { get; }
		public bool CanRender { get; set; }
		public bool ShouldDestroy { get; set; }

		public void Setup();
		public void Render(float delta);

		public bool IsSameWindow(Window window);
	}
}