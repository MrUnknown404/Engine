using Engine4.Client.Rendering;

namespace Engine4.Client.Graphics.Vulkan;

public sealed class VulkanRenderer : Renderer {
	internal VulkanRenderer(RenderTarget renderTarget, VulkanProvider graphicsProvider, params RenderPass[] renderPasses) : base(renderTarget, graphicsProvider, renderPasses) { }

	protected override void Render(float delta) {
		if (BeginFrame()) {
			UpdateBuffers(delta);
			DrawFrame();
			EndFrame();
			PresentFrame();
		}
	}

	private bool BeginFrame() => throw new NotImplementedException(); // TODO
	private void UpdateBuffers(float delta) => throw new NotImplementedException(); // TODO
	private void DrawFrame() => throw new NotImplementedException(); // TODO
	private void EndFrame() => throw new NotImplementedException(); // TODO
	private void PresentFrame() => throw new NotImplementedException(); // TODO
}