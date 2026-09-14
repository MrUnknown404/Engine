using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class PhysicalGpu {
	public VkPhysicalDevice VkPhysicalDevice { get; } // TODO private
	public VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; } // TODO private
	public VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; } // TODO private

	public PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, PhysicalGpuProperties properties) {
		VkPhysicalDevice = vkPhysicalDevice;
		PhysicalDeviceProperties2 = properties.PhysicalDeviceProperties2;
		PhysicalDeviceFeatures2 = properties.PhysicalDeviceFeatures2;
	}
}