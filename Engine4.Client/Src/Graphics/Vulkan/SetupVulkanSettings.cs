using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine4.Client.Graphics.Vulkan;

public class SetupVulkanSettings {
	public string[] RequiredInstanceLayerProperties { get; init; } = Array.Empty<string>();
	public string[] RequiredInstanceExtensionProperties { get; init; } = Array.Empty<string>();
	public string[] RequiredDeviceExtensionProperties { get; init; } = Array.Empty<string>();

	public VkPresentModeKHR PresentMode { get; init; } = VkPresentModeKHR.PresentModeImmediateKhr; // TODO support setting this at runtime
	public bool AllowEnableAnisotropy { get; init; } = true;

	public byte MaxFramesInFlight { get; init; } = 2;

	// gpu selection
	public IsPhysicalDeviceSuitableDelegate? IsPhysicalDeviceSuitable { get; init; }
	public SelectGpuDelegate? GetManualGpuFunc { get; init; } // if this is null use RateGpuSuitability
	public RateGpuSuitabilityDelegate RateGpuSuitability { get; init; } = DefaultRateGpuSuitability;

	// debugging
	public VkDebugUtilsMessageSeverityFlagBitsEXT EnabledDebugMessageSeverities { get; init; } = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt |
																								 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt |
																								 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt |
																								 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt;

	public VkDebugUtilsMessageTypeFlagBitsEXT EnabledDebugMessageTypes { get; init; } = VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt |
																						VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt |
																						VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt;

	[MustUseReturnValue]
	public VulkanSettings ToReadonly(bool isGlfwEnabled) {
		// engine extensions
		HashSet<string> requiredInstanceLayerProperties = new([
#if DEBUG
				"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
		]);

		requiredInstanceLayerProperties.UnionWith(RequiredInstanceLayerProperties);

		HashSet<string> requiredInstanceExtensionProperties = new([
				Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
				Vk.ExtDebugUtilsExtensionName,
#endif
		]);

		requiredInstanceExtensionProperties.UnionWith(RequiredInstanceExtensionProperties);

		HashSet<string> requiredDeviceExtensionProperties = new([
				Vk.KhrSwapchainExtensionName, //
		]);

		requiredDeviceExtensionProperties.UnionWith(RequiredDeviceExtensionProperties);

		if (isGlfwEnabled) { requiredInstanceExtensionProperties.UnionWith(GLFW.GetRequiredInstanceExtensions()); }

		return new() {
				RequiredInstanceLayerProperties = requiredInstanceLayerProperties.ToArray(),
				RequiredInstanceExtensionProperties = requiredInstanceExtensionProperties.ToArray(),
				RequiredDeviceExtensionProperties = requiredDeviceExtensionProperties.ToArray(),
				PresentMode = PresentMode,
				AllowEnableAnisotropy = AllowEnableAnisotropy,
				MaxFramesInFlight = MaxFramesInFlight,
				GetManualGpuFunc = GetManualGpuFunc,
				RateGpuSuitability = RateGpuSuitability,
		};
	}

	internal void PrintValues() {
		// TODO print values
	}

	private static int DefaultRateGpuSuitability(SurfaceReadyPhysicalGpu physicalGpu) {
		VkPhysicalDeviceProperties deviceProperties = physicalGpu.Properties2.properties;
		int score = 0;

		if (deviceProperties.deviceType == VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu) { score += 1000; } // prioritize discreate
		score += (int)deviceProperties.limits.maxImageDimension2D; // prefer larger max images

		return score;
	}
}