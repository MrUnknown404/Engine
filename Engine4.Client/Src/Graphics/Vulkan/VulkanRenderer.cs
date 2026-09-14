using Engine4.Client.Rendering;
using JetBrains.Annotations;

namespace Engine4.Client.Graphics.Vulkan;

public sealed class VulkanRenderer {
	public ulong FrameCount { get; private set; }

	private readonly VulkanResourceManager resourceManager;
	private readonly List<RenderPass> renderPasses; // TODO make sure this supports adding/removing at runtime
	private readonly RenderTarget renderTarget; // TODO eventually allow multiple targets

	internal VulkanRenderer(VulkanManager vulkanManager, RenderTarget renderTarget, params RenderPass[] renderPasses) {
		resourceManager = vulkanManager.ResourceManager;
		this.renderTarget = renderTarget;
		this.renderPasses = new(renderPasses);
	}

	internal void InternalRender(float delta) {
		Render(delta);
		FrameCount++;
	}

	private void Render(float delta) {
		if (TryBeginFrame()) {
			UpdateBuffers(delta);
			SyncResources(); // TODO how do i handle this?

			DrawFrame();
			EndFrame();
			PresentFrame();
		}
	}

	[MustUseReturnValue]
	private bool TryBeginFrame() => throw new NotImplementedException(); // TODO

	private void UpdateBuffers(float delta) => throw new NotImplementedException(); // TODO
	private void SyncResources() => throw new NotImplementedException(); // TODO
	private void DrawFrame() => throw new NotImplementedException(); // TODO
	private void EndFrame() => throw new NotImplementedException(); // TODO
	private void PresentFrame() => throw new NotImplementedException(); // TODO
}