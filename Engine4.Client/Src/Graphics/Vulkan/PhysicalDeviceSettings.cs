using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public class PhysicalDeviceSettings {
	public VkPhysicalDeviceVulkan11Features Vulkan11Features { get; init; } = new();
	public VkPhysicalDeviceVulkan12Features Vulkan12Features { get; init; } = new();
	public VkPhysicalDeviceVulkan13Features Vulkan13Features { get; init; } = new();
	public VkPhysicalDeviceVulkan14Features Vulkan14Features { get; init; } = new();
	public VkPhysicalDeviceFeatures PhysicalDeviceFeatures { get; init; } = new();
}