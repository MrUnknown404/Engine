using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public readonly record struct PhysicalGpuProperties {
	public required VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; init; }
	public required VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; init; }

	[SetsRequiredMembers]
	public PhysicalGpuProperties(VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2) {
		PhysicalDeviceProperties2 = physicalDeviceProperties2;
		PhysicalDeviceFeatures2 = physicalDeviceFeatures2;
	}
}