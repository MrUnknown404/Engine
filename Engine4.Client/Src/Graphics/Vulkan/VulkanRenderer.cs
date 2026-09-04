using Engine4.Client.Rendering;

namespace Engine4.Client.Graphics.Vulkan;

public sealed class VulkanRenderer : Renderer {
	internal VulkanRenderer(RenderTarget renderTarget, VulkanProvider graphicsProvider, params RenderPass[] renderPasses) : base(renderTarget, graphicsProvider, renderPasses) { }

	protected internal override bool BeginFrame() => throw new NotImplementedException(); // TODO
	protected internal override void UpdateBuffers(float delta) => throw new NotImplementedException(); // TODO
	protected internal override void DrawFrame() => throw new NotImplementedException(); // TODO
	protected internal override void EndFrame() => throw new NotImplementedException(); // TODO
	protected internal override void PresentFrame() => throw new NotImplementedException(); // TODO
}