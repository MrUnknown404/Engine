using Engine4.Client.Graphics.Vulkan;

namespace Engine4.Client.Rendering;

public abstract class Renderer {
	public VulkanResourceManager VulkanResourceManager { get; }
	public ulong FrameCount { get; private set; }

	protected List<RenderPass> RenderPasses { get; } // TODO make sure this supports adding/removing at runtime
	protected RenderTarget RenderTarget { get; } // TODO eventually allow multiple targets

	protected Renderer(RenderTarget renderTarget, VulkanResourceManager vulkanResourceManager, params RenderPass[] renderPasses) {
		RenderTarget = renderTarget;
		VulkanResourceManager = vulkanResourceManager;
		RenderPasses = new(renderPasses);
	}

	internal void InternalRender(float delta) {
		Render(delta);
		FrameCount++;
	}

	protected abstract void Render(float delta);
}