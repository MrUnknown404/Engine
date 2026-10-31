using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class PhysicalGpu {
	public VkPhysicalDevice VkPhysicalDevice { get; }
	public VkPhysicalDeviceProperties2 Properties2 { get; }
	public VkPhysicalDeviceFeatures2 Features2 { get; }

	protected PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, VkPhysicalDeviceProperties2 properties2, VkPhysicalDeviceFeatures2 features2) {
		VkPhysicalDevice = vkPhysicalDevice;
		Properties2 = properties2;
		Features2 = features2;
	}

	internal PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, PhysicalGpuProperties properties) : this(vkPhysicalDevice, properties.PhysicalDeviceProperties2, properties.PhysicalDeviceFeatures2) { }
}