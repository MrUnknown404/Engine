using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

public abstract class GraphicsRenderPass3 : RenderPass3 {
	protected internal VkClearColorValue? ClearColor { get; protected init; }

	protected internal RenderGraph3.TextureHandle? DepthImageHandle { get; private set; } // should only graphics have this stuff?
	protected internal VkClearDepthStencilValue? DepthStencil { get; private set; }

	public void SetDepthImage(RenderGraph3.TextureHandle depthImageHandle, VkClearDepthStencilValue depthStencil) {
		DepthImageHandle = depthImageHandle;
		DepthStencil = depthStencil;
	}

	public void DrawIndexed() => throw new NotImplementedException(); // TODO ?
}