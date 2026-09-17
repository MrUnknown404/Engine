using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Graphics.Vulkan;

public class SurfaceReadyPhysicalGpu : PhysicalGpu {
	private readonly Surface surface; // TODO use this? or remove it?

	internal QueueFamilyIndices QueueFamilyIndices { get; }

	internal SurfaceReadyPhysicalGpu(PhysicalGpu physicalGpu, Surface surface, QueueFamilyIndices queueFamilyIndices) : base(physicalGpu.VkPhysicalDevice, physicalGpu.PhysicalDeviceProperties2,
		physicalGpu.PhysicalDeviceFeatures2) {
		this.surface = surface;
		QueueFamilyIndices = queueFamilyIndices;
	}
}