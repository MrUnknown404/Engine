using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public class PhysicalGpu {
	public VkPhysicalDevice VkPhysicalDevice { get; } // TODO private
	public VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; } // ^
	public VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; } // ^

	public PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, PhysicalGpuProperties properties) {
		VkPhysicalDevice = vkPhysicalDevice;
		PhysicalDeviceProperties2 = properties.PhysicalDeviceProperties2;
		PhysicalDeviceFeatures2 = properties.PhysicalDeviceFeatures2;
	}
}