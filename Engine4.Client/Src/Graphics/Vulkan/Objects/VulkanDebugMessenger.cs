using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine4.IO;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class VulkanDebugMessenger : VulkanResource {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Vulkan);

	private static readonly string[] MessageTypesCache;

	private readonly VkDebugUtilsMessengerEXT vkDebugMessenger;
	public VkDebugUtilsMessengerEXT VkDebugMessenger => vkDebugMessenger;
	public override ulong Handle => VkDebugMessenger.Handle;

	private readonly VulkanInstance vulkanInstance;

	static VulkanDebugMessenger() {
		const uint HighestValue = (uint)(VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt |
										 VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt |
										 VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt |
										 VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeDeviceAddressBindingBitExt);

		MessageTypesCache = new string[HighestValue];
		MessageTypesCache[0] = "Unknown";

		for (int i = 1; i < HighestValue; i++) { MessageTypesCache[i] = ((VkDebugUtilsMessageTypeFlagBitsEXT)i).ToString().Replace("DebugUtilsMessageType", string.Empty).Replace("BitExt", string.Empty); }
	}

	internal VulkanDebugMessenger(VulkanInstance vulkanInstance, SetupVulkanSettings setupVulkanSettings) {
		this.vulkanInstance = vulkanInstance;

		VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(setupVulkanSettings.EnabledDebugMessageSeverities, setupVulkanSettings.EnabledDebugMessageTypes);
		fixed (VkDebugUtilsMessengerEXT* pVkDebugMessenger = &vkDebugMessenger) {
			VkH.ThrowIfNotSuccess(Vk.CreateDebugUtilsMessengerEXT(vulkanInstance.VkInstance, &messengerCreateInfo, null, pVkDebugMessenger), "Failed to create debug messenger");
		}
	}

	protected internal override void Cleanup() => Vk.DestroyDebugUtilsMessengerEXT(vulkanInstance.VkInstance, VkDebugMessenger, null);

	[MustUseReturnValue]
	internal static VkDebugUtilsMessengerCreateInfoEXT CreateDebugUtilsMessengerCreateInfoEXT(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType) =>
			new() { messageSeverity = messageSeverity, messageType = messageType, pfnUserCallback = &VulkanDebugCallback, };

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl), })]
	private static int VulkanDebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
		string message = $"[{MessageTypesCache[(uint)messageType]}] - {Marshal.PtrToStringAnsi((IntPtr)pCallbackData->pMessage) ?? throw new Exception()}";

		switch (messageSeverity) {
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt: Logger.Error(message); break;
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt: Logger.Warn(message); break;
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt: Logger.Info(message); break;
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt: Logger.Debug(message); break;
			default: Logger.Warn($"Got unknown severity. {message}"); break;
		}

		return 0;
	}
}