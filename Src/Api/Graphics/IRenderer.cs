using Engine3.Graphics;

namespace Engine3.Api.Graphics {
	public interface IRenderer : IDestroyable {
		public ulong FrameCount { get; }
		public bool CanRender { get; set; }
		public bool ShouldDestroy { get; set; }

		public void Setup();
		public void Render(float delta);

		public bool IsSameWindow(Window window);
	}
}