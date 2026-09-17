using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class UnboundPhysicalGpu {
	internal VkPhysicalDevice VkPhysicalDevice { get; } // TODO private
	internal VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; } // TODO private
	internal VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; } // TODO private

	protected UnboundPhysicalGpu(VkPhysicalDevice vkPhysicalDevice, VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2) {
		VkPhysicalDevice = vkPhysicalDevice;
		PhysicalDeviceProperties2 = physicalDeviceProperties2;
		PhysicalDeviceFeatures2 = physicalDeviceFeatures2;
	}

	public UnboundPhysicalGpu(VkPhysicalDevice vkPhysicalDevice, PhysicalGpuProperties properties) {
		VkPhysicalDevice = vkPhysicalDevice;
		PhysicalDeviceProperties2 = properties.PhysicalDeviceProperties2;
		PhysicalDeviceFeatures2 = properties.PhysicalDeviceFeatures2;
	}
}