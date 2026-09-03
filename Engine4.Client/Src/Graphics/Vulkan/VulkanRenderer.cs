using Engine4.Client.Rendering;

namespace Engine4.Client.Graphics.Vulkan;

public class VulkanRenderer : Renderer {
	internal VulkanRenderer(RenderTarget renderTarget, VulkanProvider graphicsProvider, params RenderPass[] renderPasses) : base(renderTarget, graphicsProvider, renderPasses) { }

	public override bool BeginFrame() => throw new NotImplementedException(); // TODO
	public override void UpdateBuffers(float delta) => throw new NotImplementedException(); // TODO
	public override void DrawFrame() => throw new NotImplementedException(); // TODO
	public override void EndFrame() => throw new NotImplementedException(); // TODO
	public override void PresentFrame() => throw new NotImplementedException(); // TODO
}