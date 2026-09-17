using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public class PhysicalGpu {
	internal VkPhysicalDevice VkPhysicalDevice { get; }
	internal VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; }
	internal VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; }

	protected PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2) {
		VkPhysicalDevice = vkPhysicalDevice;
		PhysicalDeviceProperties2 = physicalDeviceProperties2;
		PhysicalDeviceFeatures2 = physicalDeviceFeatures2;
	}

	internal PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, PhysicalGpuProperties properties) {
		VkPhysicalDevice = vkPhysicalDevice;
		PhysicalDeviceProperties2 = properties.PhysicalDeviceProperties2;
		PhysicalDeviceFeatures2 = properties.PhysicalDeviceFeatures2;
	}
}