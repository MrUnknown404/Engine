#if DEBUG

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public static unsafe partial class VkH {
		[MustUseReturnValue]
		public static VkDebugUtilsMessengerEXT CreateDebugMessenger(VkInstance vkInstance, VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType) {
			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(messageSeverity, messageType);
			VkDebugUtilsMessengerEXT debugMessenger;
			return Vk.CreateDebugUtilsMessengerEXT(vkInstance, &messengerCreateInfo, null, &debugMessenger) != VkResult.Success ? throw new VulkanException("Failed to create Vulkan Debug Messenger") : debugMessenger;
		}

		[MustUseReturnValue]
		public static bool CheckSupportForRequiredValidationLayers(string[] requiredValidationLayers) {
			ReadOnlySpan<VkLayerProperties> availableLayerProperties = EnumerateInstanceLayerProperties();
			if (availableLayerProperties.Length == 0) { throw new VulkanException("Could not find any instance layer properties"); }

			foreach (string wantedLayer in requiredValidationLayers) {
				bool layerFound = false;

				foreach (VkLayerProperties layerProperties in availableLayerProperties) {
					ReadOnlySpan<byte> layerName = layerProperties.layerName;
					if (Encoding.UTF8.GetString(layerName[..layerName.IndexOf((byte)0)]) == wantedLayer) {
						layerFound = true;
						break;
					}
				}

				if (!layerFound) { return false; }
			}

			return true;
		}

		public static void PrintInstanceExtensions(VkExtensionProperties[] instanceExtensionProperties) {
			Logger.Debug("The following instance extensions are available:");
			foreach (VkExtensionProperties extensionProperties in instanceExtensionProperties) {
				ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
				Logger.Debug($"- {Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)])}");
			}
		}

		[MustUseReturnValue]
		private static VkLayerProperties[] EnumerateInstanceLayerProperties() {
			uint layerCount;
			Vk.EnumerateInstanceLayerProperties(&layerCount, null);

			if (layerCount == 0) { return Array.Empty<VkLayerProperties>(); }

			VkLayerProperties[] layerProperties = new VkLayerProperties[layerCount];
			fixed (VkLayerProperties* layerPropertiesPtr = layerProperties) {
				Vk.EnumerateInstanceLayerProperties(&layerCount, layerPropertiesPtr);
				return layerProperties;
			}
		}

		private static VkDebugUtilsMessengerCreateInfoEXT CreateDebugUtilsMessengerCreateInfoEXT(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType) =>
				new() { messageSeverity = messageSeverity, messageType = messageType, pfnUserCallback = &DebugCallback, };

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl), })]
		private static int DebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
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