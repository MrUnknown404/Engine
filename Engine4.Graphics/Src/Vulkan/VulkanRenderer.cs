using Engine4.Graphics.Rendering;

namespace Engine4.Graphics.Vulkan;

public class VulkanRenderer : Renderer {
	public VulkanRenderer(RenderTarget renderTarget, IGraphicsApiProvider graphicsProvider, params RenderPass[] renderPasses) : base(GraphicsApi.Vulkan, renderTarget, graphicsProvider, renderPasses) { }

	public override bool BeginFrame() => throw new NotImplementedException(); // TODO
	public override void UpdateBuffers(float delta) => throw new NotImplementedException(); // TODO
	public override void DrawFrame() => throw new NotImplementedException(); // TODO
	public override void EndFrame() => throw new NotImplementedException(); // TODO
	public override void PresentFrame() => throw new NotImplementedException(); // TODO
}