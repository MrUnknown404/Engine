using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Utility.Math;
using USharpLibs.Common.Math;
using Semaphore = Engine4.Client.Graphics.Vulkan.Objects.Semaphore;

namespace Engine4.Client.Rendering;

// TODO save to file

public sealed class TextureRenderTarget : RenderTarget {
	internal override SurfaceReadyPhysicalGpu PhysicalGpu => throw new NotImplementedException(); // TODO impl
	internal override LogicalGpu LogicalGpu => throw new NotImplementedException();

	internal TextureRenderTarget(Color3 clearColor) : base(clearColor) { } // TODO how is this going to work? when a renderer is done it has the final image. how do i get that?

	protected internal override bool TryBeginFrame(VulkanRenderer.FrameInFlight frame) => throw new NotImplementedException(); // TODO

	protected internal override void CmdBeginRendering(GraphicsCommandBuffer graphicsCommandBuffer, DepthImage? depthImage) => throw new NotImplementedException();
	protected internal override void CmdEndRendering(GraphicsCommandBuffer graphicsCommandBuffer) => throw new NotImplementedException();

	protected internal override void PresentFrame(VulkanRenderer.FrameInFlight frame) => throw new NotImplementedException(); // TODO

	protected internal override Semaphore GetSignalSemaphore() => throw new NotImplementedException();

	public override Vec2<ushort> GetFrameBufferSize() => throw new NotImplementedException(); // TODO

	protected internal override void Cleanup() { }
}