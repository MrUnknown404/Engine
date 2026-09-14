using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Graphics.Vulkan;

// TODO idk. i may rename this
public class BoundPhysicalGpu : UnboundPhysicalGpu {
	private readonly VulkanSurface surface;

	public QueueFamilyIndices QueueFamilyIndices { get; }

	internal BoundPhysicalGpu(UnboundPhysicalGpu unboundPhysicalGpu, VulkanSurface surface, QueueFamilyIndices queueFamilyIndices) : base(unboundPhysicalGpu.VkPhysicalDevice, unboundPhysicalGpu.PhysicalDeviceProperties2,
		unboundPhysicalGpu.PhysicalDeviceFeatures2) {
		this.surface = surface;
		QueueFamilyIndices = queueFamilyIndices;
	}
}