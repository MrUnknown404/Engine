namespace Engine4.Client.Graphics.Vulkan.Objects;

// TODO idk. i may rename this
public class BoundPhysicalGpu : UnboundPhysicalGpu {
	private readonly Surface surface;

	public QueueFamilyIndices QueueFamilyIndices { get; }

	internal BoundPhysicalGpu(UnboundPhysicalGpu unboundPhysicalGpu, Surface surface, QueueFamilyIndices queueFamilyIndices) : base(unboundPhysicalGpu.VkPhysicalDevice, unboundPhysicalGpu.PhysicalDeviceProperties2,
		unboundPhysicalGpu.PhysicalDeviceFeatures2) {
		this.surface = surface;
		QueueFamilyIndices = queueFamilyIndices;
	}
}