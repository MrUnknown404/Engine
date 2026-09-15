using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Rendering;

public sealed class WindowRenderTarget : RenderTarget {
	public override BoundPhysicalGpu PhysicalGpu { get; }
	public override LogicalGpu LogicalGpu { get; }

	private readonly Window window;
	private readonly VulkanSurface surface;
	private readonly SwapChain swapChain;

	internal WindowRenderTarget(Window window, VulkanManager vulkanManager) {
		this.window = window;

		// TODO logging
		surface = new(vulkanManager.VulkanInstance, window);
		BoundPhysicalGpu[] capableGpus = vulkanManager.GetCapableGpus(surface);
		PhysicalGpu = vulkanManager.SelectGpu(capableGpus) ?? throw new Exception(); // TODO exception
		LogicalGpu = new(PhysicalGpu, vulkanManager);
		swapChain = new(window, PhysicalGpu, LogicalGpu, surface, vulkanManager.PresentMode);
	}

	protected internal override void Cleanup() {
		surface.Cleanup();
		LogicalGpu.Cleanup();

		// TODO call
	}
}