using Engine3.Core.Utility;

namespace Engine3.Core.Graphics;

public abstract class ConsoleRenderer : Renderer { // TODO impl
	protected ConsoleRenderer() : base(GraphicsApi.Console) { }

	protected override void PrepareRender() { }
	protected override void TryCleanupResources() { }
	protected override bool TryNextFrame() => true;

	protected override void BeginFrame() { }

	protected override void PrepareCleanup() { }
	protected override void Cleanup() { }
}