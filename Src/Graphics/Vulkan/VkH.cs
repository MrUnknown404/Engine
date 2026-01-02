using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public static unsafe partial class VkH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static readonly List<string> RequiredValidationLayers = new();
		public static readonly List<string> RequiredExtensions = new();

		public static VkDebugUtilsMessageSeverityFlagBitsEXT EnabledDebugMessageSeverities {
			get;
			set {
				if (Engine3.WasVulkanSetup) { Logger.Warn("Attempted to set EnabledDebugMessageSeverities too late"); }
				field = value;
			}
		} = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt |
			VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt |
			VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt |
			VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt;

		public static VkDebugUtilsMessageTypeFlagBitsEXT EnabledDebugMessageTypes {
			get;
			set {
				if (Engine3.WasVulkanSetup) { Logger.Warn("Attempted to set EnabledDebugMessageTypes too late"); }
				field = value;
			}
		} = VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt |
			VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt |
			VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt |
			VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeDeviceAddressBindingBitExt;

		/// <summary>
		/// Helper method for extracting version information out of a packed uint following Vulkan's Version Specification.<br/><br/>
		/// The variant is packed into bits 31-29.<br/>
		/// The major version is packed into bits 28-22.<br/>
		/// The minor version number is packed into bits 21-12.<br/>
		/// The patch version number is packed into bits 11-0.<br/>
		/// </summary>
		/// <param name="version">  </param>
		/// <param name="variant"> 3-bit integer </param>
		/// <param name="major"> 7-bit integer </param>
		/// <param name="minor"> 10-bit integer </param>
		/// <param name="patch"> 12-bit integer </param>
		/// <seealso href="https://docs.vulkan.org/spec/latest/chapters/extensions.html#extendingvulkan-coreversions">Relevant Vulkan Specification</seealso>
		public static void GetApiVersion(uint version, out byte variant, out byte major, out ushort minor, out ushort patch) {
			variant = (byte)(version >> 29);
			major = (byte)((version >> 22) & 0x7FU);
			minor = (ushort)((version >> 12) & 0x3FFU);
			patch = (ushort)(version & 0xFFFU);
		}

		public static VkPhysicalDeviceProperties2 GetPhysicalDeviceProperties(VkPhysicalDevice device) {
			VkPhysicalDeviceProperties2 deviceProperties2 = new();
			Vk.GetPhysicalDeviceProperties2(device, &deviceProperties2);
			return deviceProperties2;
		}

		public static VkPhysicalDeviceFeatures2 GetPhysicalDeviceFeatures(VkPhysicalDevice device) {
			VkPhysicalDeviceFeatures2 deviceFeatures2 = new();
			Vk.GetPhysicalDeviceFeatures2(device, &deviceFeatures2);
			return deviceFeatures2;
		}

		public static ReadOnlySpan<VkQueueFamilyProperties2> GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice device) {
			uint queueFamilyPropertyCount = 0;
			Vk.GetPhysicalDeviceQueueFamilyProperties2(device, &queueFamilyPropertyCount, null);

			if (queueFamilyPropertyCount == 0) { return ReadOnlySpan<VkQueueFamilyProperties2>.Empty; }

			VkQueueFamilyProperties2[] queueFamilyProperties = new VkQueueFamilyProperties2[queueFamilyPropertyCount];
			for (int i = 0; i < queueFamilyPropertyCount; i++) { queueFamilyProperties[i] = new() { sType = VkStructureType.StructureTypeQueueFamilyProperties2, }; }

			fixed (VkQueueFamilyProperties2* queueFamilyPropertiesPtr = queueFamilyProperties) {
				Vk.GetPhysicalDeviceQueueFamilyProperties2(device, &queueFamilyPropertyCount, queueFamilyPropertiesPtr);
				return queueFamilyProperties;
			}
		}

		public static ReadOnlySpan<VkExtensionProperties> EnumerateInstanceExtensionProperties() {
			uint extensionCount;
			Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, null);

			if (extensionCount == 0) { return ReadOnlySpan<VkExtensionProperties>.Empty; }

			VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
			fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
				Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, extensionPropertiesPtr);
				return extensionProperties;
			}
		}

		public static ReadOnlySpan<VkPhysicalDevice> EnumeratePhysicalDevices(VkInstance vkInstance) {
			uint deviceCount;
			Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, null);

			if (deviceCount == 0) { return ReadOnlySpan<VkPhysicalDevice>.Empty; }

			VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
			fixed (VkPhysicalDevice* physicalDevicesPtr = physicalDevices) {
				Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, physicalDevicesPtr);
				return physicalDevices;
			}
		}
	}
}