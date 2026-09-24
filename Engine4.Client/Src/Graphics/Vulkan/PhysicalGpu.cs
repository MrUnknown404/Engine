using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public unsafe class PhysicalGpu {
	internal VkPhysicalDevice VkPhysicalDevice { get; }
	internal VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; }
	internal VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; }
	internal VkPhysicalDeviceMemoryProperties2 PhysicalDeviceMemoryProperties2 { get; }

	protected PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2) {
		VkPhysicalDevice = vkPhysicalDevice;
		PhysicalDeviceProperties2 = physicalDeviceProperties2;
		PhysicalDeviceFeatures2 = physicalDeviceFeatures2;

		VkPhysicalDeviceMemoryProperties2 physicalDeviceMemoryProperties2 = new();
		Vk.GetPhysicalDeviceMemoryProperties2(vkPhysicalDevice, &physicalDeviceMemoryProperties2);
		PhysicalDeviceMemoryProperties2 = physicalDeviceMemoryProperties2;
	}

	internal PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, PhysicalGpuProperties properties) : this(vkPhysicalDevice, properties.PhysicalDeviceProperties2, properties.PhysicalDeviceFeatures2) { }
}