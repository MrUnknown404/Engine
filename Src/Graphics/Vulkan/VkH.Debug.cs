#if DEBUG

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public static unsafe partial class VkH {
		static VkH() {
			RequiredValidationLayers.Add("VK_LAYER_KHRONOS_validation"); // if OpenTK defines this somewhere, i could not find it
			RequiredInstanceExtensions.Add(Vk.ExtDebugUtilsExtensionName);
		}

		public static ReadOnlySpan<VkLayerProperties> EnumerateInstanceLayerProperties() {
			uint layerCount;
			Vk.EnumerateInstanceLayerProperties(&layerCount, null);

			if (layerCount == 0) { return ReadOnlySpan<VkLayerProperties>.Empty; }

			VkLayerProperties[] layerProperties = new VkLayerProperties[layerCount];
			fixed (VkLayerProperties* layerPropertiesPtr = layerProperties) {
				Vk.EnumerateInstanceLayerProperties(&layerCount, layerPropertiesPtr);
				return layerProperties;
			}
		}

		public static VkDebugUtilsMessengerCreateInfoEXT CreateVkDebugUtilsMessengerCreateInfoEXT() =>
				new() { messageSeverity = EnabledDebugMessageSeverities, messageType = EnabledDebugMessageTypes, pfnUserCallback = &VulkanDebugCallback, };

		[MustUseReturnValue]
		public static VkDebugUtilsMessengerEXT CreateDebugMessenger(VkInstance vkInstance) {
			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateVkDebugUtilsMessengerCreateInfoEXT();

			VkDebugUtilsMessengerEXT debugMessenger;
			return Vk.CreateDebugUtilsMessengerEXT(vkInstance, &messengerCreateInfo, null, &debugMessenger) != VkResult.Success ?
					throw // VkInstance shouldn't be null here
							new VulkanException("Failed to create Vulkan Debug Messenger") :
					debugMessenger;
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl), })]
		private static int VulkanDebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
			string message = $"[Vulkan Callback] [{messageType switch {
					VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeDeviceAddressBindingBitExt => "Device Address Binding",
					VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt => "General",
					VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt => "Performance",
					VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt => "Validation",
					_ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null),
			}}] - {Marshal.PtrToStringAnsi((IntPtr)pCallbackData->pMessage) ?? throw new Exception()}";

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
}

#endif