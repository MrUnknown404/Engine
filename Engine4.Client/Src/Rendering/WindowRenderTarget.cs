using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Rendering;

public class WindowRenderTarget : RenderTarget {
	private readonly Window window;
	private readonly VulkanSurface surface;
	private readonly BoundPhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;

	internal WindowRenderTarget(Window window, VulkanManager vulkanManager) {
		this.window = window;

		// TODO logging
		surface = new(vulkanManager.VulkanInstance, window);
		BoundPhysicalGpu[] capableGpus = vulkanManager.GetCapableGpus(surface);
		physicalGpu = vulkanManager.SelectGpu(capableGpus) ?? throw new Exception(); // TODO exception
		logicalGpu = new(physicalGpu, vulkanManager);
	}

	protected internal override void Cleanup() {
		surface.Cleanup();
		logicalGpu.Cleanup();

		// TODO call
	}
}