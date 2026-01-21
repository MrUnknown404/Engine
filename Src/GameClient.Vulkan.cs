using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3 {
	public abstract partial class GameClient {
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

		public byte MaxFramesInFlight { get; init; } = 2;

		public VkInstance? VkInstance { get; private set; }
		public VkPhysicalDevice[] PhysicalDevices { get; private set; } = Array.Empty<VkPhysicalDevice>();
		public bool WasVulkanSetup { get; private set; }

#if DEBUG
		private static VkDebugUtilsMessengerEXT? vkDebugMessenger;
#endif

		private unsafe void SetupVulkan() {
			Logger.Debug("Loading Vulkan library...");
			VKLoader.Init();

			uint apiVersion;
			Vk.EnumerateInstanceVersion(&apiVersion);
			Logger.Debug($"- Version: {apiVersion} ({Vk.API_VERSION_MAJOR(apiVersion)}.{Vk.API_VERSION_MINOR(apiVersion)}.{Vk.API_VERSION_PATCH(apiVersion)})");

#if DEBUG
			VkLayerProperties[] availableLayerProperties = VkH.EnumerateInstanceLayerProperties();
			if (availableLayerProperties.Length == 0) { throw new VulkanException("Could not find any instance layer properties"); }
			if (!VkH.CheckSupportForRequiredValidationLayers(availableLayerProperties, GetAllRequiredValidationLayers())) { throw new VulkanException("Requested validation layers are not available"); }
#endif

			VkExtensionProperties[] instanceExtensionProperties = VkH.GetInstanceExtensionProperties();
			if (instanceExtensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }
			if (!VkH.CheckSupportForRequiredInstanceExtensions(instanceExtensionProperties, GetAllRequiredInstanceExtensions())) { throw new VulkanException("Requested instance extensions are not available"); }

#if DEBUG
			VkH.PrintInstanceExtensions(instanceExtensionProperties);
#endif

			VkInstance = VkH.CreateVulkanInstance(this, Engine3.Name, Version, Engine3.Version);
			VKLoader.SetInstance(VkInstance.Value);
			Logger.Info("Created Vulkan instance");

#if DEBUG
			vkDebugMessenger = VkH.CreateDebugMessenger(VkInstance.Value, EnabledDebugMessageSeverities, EnabledDebugMessageTypes);
			Logger.Debug("Created Vulkan Debug Messenger");
#endif

			PhysicalDevices = VkH.GetPhysicalDevices(VkInstance.Value);
			Logger.Debug("Created Physical Devices");

			WasVulkanSetup = true;
		}

		public string[] GetAllRequiredValidationLayers() {
			HashSet<string> allRequiredValidationLayers = new();
			allRequiredValidationLayers.UnionWith(Engine3.RequiredValidationLayers);
			allRequiredValidationLayers.UnionWith(RequiredValidationLayers);
			return allRequiredValidationLayers.ToArray();
		}

		public string[] GetAllRequiredInstanceExtensions() {
			HashSet<string> allInstanceExtensions = new();
			allInstanceExtensions.UnionWith(Toolkit.Vulkan.GetRequiredInstanceExtensions().ToArray()); // no span support??
			allInstanceExtensions.UnionWith(Engine3.RequiredInstanceExtensions);
			allInstanceExtensions.UnionWith(RequiredInstanceExtensions);
			return allInstanceExtensions.ToArray();
		}

		public string[] GetAllRequiredDeviceExtensions() {
			HashSet<string> allDeviceExtensions = new();
			allDeviceExtensions.UnionWith(Engine3.RequiredDeviceExtensions);
			allDeviceExtensions.UnionWith(RequiredDeviceExtensions);
			return allDeviceExtensions.ToArray();
		}

		protected internal virtual bool IsPhysicalDeviceSuitable(VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2) {
			VkPhysicalDeviceProperties deviceProperties = physicalDeviceProperties2.properties;
			return deviceProperties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu;
		}

		protected internal virtual int RateGpuSuitability(PhysicalGpu physicalGpu) {
			VkPhysicalDeviceProperties deviceProperties = physicalGpu.PhysicalDeviceProperties2.properties;
			int score = 0;

			if (deviceProperties.deviceType == VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu) { score += 1000; }
			score += (int)deviceProperties.limits.maxImageDimension2D;

			return score;
		}

		private unsafe void CleanupVulkan() {
			if (VkInstance is not { } vkInstance) { return; }

#if DEBUG
			if (vkDebugMessenger is { } debugMessage) {
				Vk.DestroyDebugUtilsMessengerEXT(vkInstance, debugMessage, null);
				vkDebugMessenger = null;
			}
#endif

			Vk.DestroyInstance(vkInstance, null);
			VkInstance = null;
		}
	}
}