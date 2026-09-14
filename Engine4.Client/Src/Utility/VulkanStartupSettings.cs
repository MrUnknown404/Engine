using Engine4.Client.Graphics.Vulkan;
using Engine4.Utility.Exceptions;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Utility;

public sealed class VulkanStartupSettings {
	public string[] RequiredInstanceLayerProperties { get; init; } = Array.Empty<string>();
	public string[] RequiredInstanceExtensionProperties { get; init; } = Array.Empty<string>();
	public string[] RequiredDeviceExtensionProperties { get; init; } = Array.Empty<string>();

	public VkDebugUtilsMessageSeverityFlagBitsEXT EnabledDebugMessageSeverities { get; init; } = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt |
																								 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt |
																								 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt |
																								 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt;

	public VkDebugUtilsMessageTypeFlagBitsEXT EnabledDebugMessageTypes { get; init; } = VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt |
																						VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt |
																						VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt;

	public VkPresentModeKHR PresentMode { get; init; } = VkPresentModeKHR.PresentModeImmediateKhr;

	public byte MaxFramesInFlight {
		get;
		init {
			if (MaxFramesInFlight == 0) { throw new Engine4Exception($"{nameof(MaxFramesInFlight)} cannot be zero"); }
			field = value;
		}
	} = 2;

	public bool AllowEnableAnisotropy { get; init; } = true;

	public IsPhysicalDeviceSuitableDelegate? IsPhysicalDeviceSuitable { get; init; }

	internal void PrintValues() {
		// TODO
	}
}