using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine4.IO;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class VulkanDebugMessenger {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Vulkan);

	private readonly VkDebugUtilsMessengerEXT vkDebugMessenger;
	private readonly VulkanInstance vulkanInstance;

	internal VulkanDebugMessenger(VulkanInstance vulkanInstance, VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType) {
		this.vulkanInstance = vulkanInstance;
		VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(messageSeverity, messageType);
		VkDebugUtilsMessengerEXT debugMessenger;

		vkDebugMessenger = Vk.CreateDebugUtilsMessengerEXT(vulkanInstance.VkInstance, &messengerCreateInfo, null, &debugMessenger) != VkResult.Success ? throw new Exception() : debugMessenger; // TODO exception
	}

	internal void Cleanup() => Vk.DestroyDebugUtilsMessengerEXT(vulkanInstance.VkInstance, vkDebugMessenger, null);

	[MustUseReturnValue]
	internal static VkDebugUtilsMessengerCreateInfoEXT CreateDebugUtilsMessengerCreateInfoEXT(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType) =>
			new() { messageSeverity = messageSeverity, messageType = messageType, pfnUserCallback = &VulkanDebugCallback, };

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl), })]
	private static int VulkanDebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
		string message = $"[{messageType switch {
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeDeviceAddressBindingBitExt => "Device Address Binding",
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt => "General",
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt => "Performance",
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt => "Validation",
				_ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null),
		}}] - {Marshal.PtrToStringAnsi((IntPtr)pCallbackData->pMessage) ?? throw new Exception()}"; // TODO merge into regular printing?

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