using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Rendering;

// TODO save to file
public sealed class TextureRenderTarget : RenderTarget {
	public override BoundPhysicalGpu PhysicalGpu => throw new NotImplementedException(); // TODO impl
	public override LogicalGpu LogicalGpu => throw new NotImplementedException();

	internal TextureRenderTarget() { } // TODO how is this going to work? when a renderer is done it has the final image. how do i get that?

	protected internal override void Cleanup() { }
}