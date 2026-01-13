using Engine3.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3 {
	public abstract partial class GameClient {
		public string[] RequiredValidationLayers { get; internal init; } = Array.Empty<string>(); // Set in Engine3#Start
		public string[] RequiredInstanceExtensions { get; internal init; } = Array.Empty<string>(); // Set in Engine3#Start
		public string[] RequiredDeviceExtensions { get; internal init; } = Array.Empty<string>(); // Set in Engine3#Start

		public VkDebugUtilsMessageSeverityFlagBitsEXT EnabledDebugMessageSeverities { get; init; } // Set in Engine3#Start
		public VkDebugUtilsMessageTypeFlagBitsEXT EnabledDebugMessageTypes { get; init; } // Set in Engine3#Start

		public byte MaxFramesInFlight { get; init; } // Set in Engine3#Start

		public string[] VkGetRequiredValidationLayers() {
			HashSet<string> allRequiredValidationLayers = new();
			allRequiredValidationLayers.UnionWith(Engine3.RequiredValidationLayers);
			allRequiredValidationLayers.UnionWith(RequiredValidationLayers);
			return allRequiredValidationLayers.ToArray();
		}

		public string[] VkGetInstanceExtensions() {
			HashSet<string> allInstanceExtensions = new();
			allInstanceExtensions.UnionWith(Toolkit.Vulkan.GetRequiredInstanceExtensions().ToArray()); // no span support??
			allInstanceExtensions.UnionWith(Engine3.RequiredInstanceExtensions);
			allInstanceExtensions.UnionWith(RequiredInstanceExtensions);
			return allInstanceExtensions.ToArray();
		}

		public string[] VkGetDeviceExtensions() {
			HashSet<string> allDeviceExtensions = new();
			allDeviceExtensions.UnionWith(Engine3.RequiredDeviceExtensions);
			allDeviceExtensions.UnionWith(RequiredDeviceExtensions);
			return allDeviceExtensions.ToArray();
		}

		protected internal virtual bool VkIsPhysicalDeviceSuitable(VkPhysicalDeviceProperties2 vkPhysicalDeviceProperties2, VkPhysicalDeviceFeatures2 vkPhysicalDeviceFeatures2) {
			VkPhysicalDeviceProperties deviceProperties = vkPhysicalDeviceProperties2.properties;
			return deviceProperties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu;
		}

		protected internal virtual int VkRateGpuSuitability(PhysicalGpu physicalGpu) {
			VkPhysicalDeviceProperties deviceProperties = physicalGpu.VkPhysicalDeviceProperties2.properties;
			int score = 0;

			if (deviceProperties.deviceType == VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu) { score += 1000; }
			score += (int)deviceProperties.limits.maxImageDimension2D;

			return score;
		}
	}
}