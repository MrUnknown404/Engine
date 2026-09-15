using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Rendering;

public abstract class RenderTarget {
	public abstract BoundPhysicalGpu PhysicalGpu { get; }
	public abstract LogicalGpu LogicalGpu { get; }

	protected internal abstract void Cleanup();
}