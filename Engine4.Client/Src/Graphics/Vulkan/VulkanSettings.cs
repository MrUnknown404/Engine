using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public class VulkanSettings {
	public required string[] RequiredInstanceLayerProperties { get; init; }
	public required string[] RequiredInstanceExtensionProperties { get; init; }
	public required string[] RequiredDeviceExtensionProperties { get; init; }

	public required VkPresentModeKHR PresentMode { get; init; } // TODO support setting this at runtime
	public required bool AllowEnableAnisotropy { get; init; }

	public required byte MaxFramesInFlight { get; init; }

	public required SelectGpuDelegate? GetManualGpuFunc { get; init; } // if this is null use RateGpuSuitability
	public required RateGpuSuitabilityDelegate RateGpuSuitability { get; init; }

	public void PrintValues() {
		// TODO print values
	}
}