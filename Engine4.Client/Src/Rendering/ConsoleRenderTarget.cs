using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Rendering;

// vulkan to console
public class ConsoleRenderTarget : RenderTarget {
	public override BoundPhysicalGpu PhysicalGpu => throw new NotImplementedException(); // TODO impl
	public override LogicalGpu LogicalGpu => throw new NotImplementedException();

	private readonly ConsoleRenderer consoleRenderer;

	internal ConsoleRenderTarget(ConsoleRenderer consoleRenderer) => this.consoleRenderer = consoleRenderer;

	public void PresentFrame() => throw new NotImplementedException(); // TODO

	protected internal override void Cleanup() { }
}