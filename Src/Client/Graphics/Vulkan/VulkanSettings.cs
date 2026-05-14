using Engine3.Exceptions;
using NLog;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Utils;

namespace Engine3.Client.Graphics.Vulkan;

public class VulkanSettings {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public string[] RequiredValidationLayers { get; init; } = Array.Empty<string>();
	public string[] RequiredInstanceExtensions { get; init; } = Array.Empty<string>();
	public string[] RequiredDeviceExtensions { get; init; } = Array.Empty<string>();

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
			if (MaxFramesInFlight == 0) { throw new Engine3VulkanException($"{nameof(MaxFramesInFlight)} cannot be zero"); }
			field = value;
		}
	} = 2;

	public bool AllowEnableAnisotropy { get; init; } = true;

	internal void Print() {
		Logger.Trace("Vulkan Graphics Backend Settings");
		Logger.Trace($"- {nameof(RequiredValidationLayers)}: {RequiredValidationLayers.ElementsAsString()}");
		Logger.Trace($"- {nameof(RequiredInstanceExtensions)}: {RequiredInstanceExtensions.ElementsAsString()}");
		Logger.Trace($"- {nameof(RequiredDeviceExtensions)}: {RequiredDeviceExtensions.ElementsAsString()}");
		Logger.Trace($"- {nameof(EnabledDebugMessageSeverities)}: {EnabledDebugMessageSeverities}");
		Logger.Trace($"- {nameof(EnabledDebugMessageTypes)}: {EnabledDebugMessageTypes}");
		Logger.Trace($"- {nameof(PresentMode)}: {PresentMode}");
		Logger.Trace($"- {nameof(MaxFramesInFlight)}: {MaxFramesInFlight}");
		Logger.Trace($"- {nameof(AllowEnableAnisotropy)}: {AllowEnableAnisotropy}");
	}
}