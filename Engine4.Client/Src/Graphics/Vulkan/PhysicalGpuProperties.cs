using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public readonly record struct PhysicalGpuProperties {
	public VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; }
	public VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; }

	public PhysicalGpuProperties(VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2) {
		PhysicalDeviceProperties2 = physicalDeviceProperties2;
		PhysicalDeviceFeatures2 = physicalDeviceFeatures2;
	}
}