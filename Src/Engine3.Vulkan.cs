using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using Engine3.Utils;
using OpenTK.Core.Native;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3 {
	public static partial class Engine3 { // TODO all these preprocessor directives are getting annoying. i may want to refactor to take those in mind
		public static bool WasVulkanSetup { get; private set; }

		public static VkInstance? VkInstance { get; private set; }
		public static VkPhysicalDevice? VkPhysicalDevice { get; private set; }
		public static VkDevice? VkLogicalDevice { get; private set; }
		public static VkQueue? VkGraphicsQueue { get; private set; }

#if DEBUG
		private static VkDebugUtilsMessengerEXT? debugMessenger;
#endif

		private static unsafe void SetupVulkan(string vulkanAppTitle, Version4 gameVersion, Version4 engineVersion, CheckIfDeviceContainsRequiredVulkanFeatures deviceFeatureCheck) {
#if DEBUG
			if (!CheckSupportForRequiredVulkanValidationLayers()) { throw new VulkanException("Requested validation layers are not available"); }
#endif
			if (!CheckSupportForRequiredVulkanExtensions()) { throw new VulkanException("Requested extensions are not available"); }

			VKLoader.Init();

			VkInstance = CreateVulkanInstance(vulkanAppTitle, gameVersion, engineVersion);
			VKLoader.SetInstance(VkInstance.Value);
			Logger.Info("Created Vulkan instance");

#if DEBUG
			CreateVulkanDebugMessenger();
			Logger.Debug("Created Vulkan Debug Messenger");
#endif

			VulkanPickPhysicalDevice(deviceFeatureCheck);
			Logger.Debug("Found physical device");
			VulkanCreateLogicalDevice();
			Logger.Debug("Created logical device");

			VkDeviceQueueInfo2 deviceQueueInfo2 = new();
			VkQueue vkGraphicsQueue;
			Vk.GetDeviceQueue2(VkLogicalDevice!.Value, &deviceQueueInfo2, &vkGraphicsQueue); // VkLogicalDevice shouldn't be null here
			VkGraphicsQueue = vkGraphicsQueue;

			WasVulkanSetup = true;
		}

		private static unsafe VkInstance CreateVulkanInstance(string title, Version4 gameVersion, Version4 engineVersion) {
			VkApplicationInfo vkApplicationInfo = new() {
					pApplicationName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(title))),
					applicationVersion = gameVersion.Packed,
					pEngineName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(Name))),
					engineVersion = engineVersion.Packed,
					apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
			};

			List<string> requiredExtensions = new();
			requiredExtensions.AddRange(Toolkit.Vulkan.GetRequiredInstanceExtensions());
			requiredExtensions.AddRange(VkH.RequiredExtensions);

			IntPtr requiredExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(requiredExtensions));
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(VkH.RequiredValidationLayers));

			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = VkH.CreateVkDebugUtilsMessengerCreateInfoEXT();
#endif

			VkInstanceCreateInfo vkCreateInfo = new() {
					pApplicationInfo = &vkApplicationInfo,
#if DEBUG
					pNext = &messengerCreateInfo,
					enabledLayerCount = (uint)VkH.RequiredValidationLayers.Count,
					ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#else
					enabledLayerCount = 0,
#endif
					enabledExtensionCount = (uint)requiredExtensions.Count,
					ppEnabledExtensionNames = (byte**)requiredExtensionsPtr,
			};

			VkInstance vkInstance;
			if (Vk.CreateInstance(&vkCreateInfo, null, &vkInstance) != VkResult.Success) { throw new VulkanException("Failed to create Vulkan instance"); }

			MarshalTk.FreeStringArrayCoTaskMem(requiredExtensionsPtr, requiredExtensions.Count);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, VkH.RequiredValidationLayers.Count);
#endif

			return vkInstance;
		}

#if DEBUG
		private static bool CheckSupportForRequiredVulkanValidationLayers() {
			ReadOnlySpan<VkLayerProperties> availableLayerProperties = VkH.EnumerateInstanceLayerProperties();
			if (availableLayerProperties.Length == 0) { throw new VulkanException("Could not find any layer properties"); }

			foreach (string wantedLayer in VkH.RequiredValidationLayers) {
				bool layerFound = false;

				foreach (VkLayerProperties layerProperties in availableLayerProperties) {
					ReadOnlySpan<byte> layerNameSpan = layerProperties.layerName;
					if (Encoding.UTF8.GetString(layerNameSpan[..layerNameSpan.IndexOf((byte)0)]) == wantedLayer) {
						layerFound = true;
						break;
					}
				}

				if (!layerFound) { return false; }
			}

			return true;
		}

#endif

		private static bool CheckSupportForRequiredVulkanExtensions() {
			ReadOnlySpan<VkExtensionProperties> extensionProperties = VkH.EnumerateInstanceExtensionProperties();
			if (extensionProperties.Length == 0) { throw new VulkanException("Could not find any extension properties"); }

			Logger.Debug("The following Vulkan extensions are available:");
			foreach (VkExtensionProperties extensionProperty in extensionProperties) {
				ReadOnlySpan<byte> extensionName = extensionProperty.extensionName;
				Logger.Debug($"- {Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)])}");
			}

			foreach (string wantedExtension in VkH.RequiredExtensions) {
				bool extensionFound = false;

				foreach (VkExtensionProperties extensionProperty in extensionProperties) {
					ReadOnlySpan<byte> extensionName = extensionProperty.extensionName;
					if (Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension) {
						extensionFound = true;
						break;
					}
				}

				if (!extensionFound) { return false; }
			}

			return true;
		}

		private static void VulkanPickPhysicalDevice(CheckIfDeviceContainsRequiredVulkanFeatures deviceFeatureCheck) {
			ReadOnlySpan<VkPhysicalDevice> devices = VkH.EnumeratePhysicalDevices(VkInstance!.Value); // VkInstance shouldn't be null here
			if (devices.Length == 0) { throw new VulkanException("Could not find any GPUs"); }

#if DEBUG
			const string PhysicalDeviceTypeEnumName = "PhysicalDeviceType";
			const string VendorIdEnumName = "VendorId";
			int physicalDeviceTypeEnumNameLength = PhysicalDeviceTypeEnumName.Length;
			int vendorIdEnumNameLength = VendorIdEnumName.Length;

			Logger.Debug("The following GPUs are available:");
			foreach (VkPhysicalDevice device in devices) {
				VkPhysicalDeviceProperties deviceProperties = VkH.GetPhysicalDeviceProperties(device).properties;

				VkH.GetApiVersion(deviceProperties.apiVersion, out _, out byte major, out ushort minor, out ushort patch);

				string vendorName = deviceProperties.vendorID is >= (uint)VkVendorId.VendorIdKhronos and <= (uint)VkVendorId.VendorIdMobileye ?
						((VkVendorId)deviceProperties.vendorID).ToString()[vendorIdEnumNameLength..] :
						deviceProperties.vendorID.ToString();

				//  TODO print better info
				Logger.Debug($"- Device Info: {deviceProperties.deviceType.ToString()[physicalDeviceTypeEnumNameLength..]}, {deviceProperties.deviceID.ToString()}, {deviceProperties.apiVersion.ToString()} ({major}.{minor}.{patch
				}), {vendorName}, {deviceProperties.driverVersion.ToString()}");
			}
#endif

			HashSet<VkPhysicalDevice> capableDevices = new();
			foreach (VkPhysicalDevice device in devices) {
				if (VulkanIsDeviceSuitable(device, deviceFeatureCheck)) {
					capableDevices.Add(device);
					break;
				}
			}

			VkPhysicalDevice? vkPhysicalDevice = VulkanSelectBestDevice(capableDevices);
			if (vkPhysicalDevice == null) { throw new VulkanException("Could not find any suitable GPUs"); }
			VkPhysicalDevice = vkPhysicalDevice;
		}

		private static bool VulkanIsDeviceSuitable(VkPhysicalDevice device, CheckIfDeviceContainsRequiredVulkanFeatures deviceFeatureCheck) {
			VkPhysicalDeviceProperties deviceProperties = VkH.GetPhysicalDeviceProperties(device).properties;
			VkPhysicalDeviceFeatures deviceFeatures = VkH.GetPhysicalDeviceFeatures(device).features;

			QueueFamilyIndices graphicsFamily = VulkanFindQueueFamilies(device);

			return deviceProperties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu &&
				   deviceFeatureCheck(deviceFeatures) &&
				   graphicsFamily != null;
		}

		private static VkPhysicalDevice? VulkanSelectBestDevice(IEnumerable<VkPhysicalDevice> capableDevices) {
			VkPhysicalDevice? bestDevice = null;
			int bestDeviceScore = 0;

			foreach (VkPhysicalDevice device in capableDevices) {
				int score = VulkanRateDeviceSuitability(device);
				if (score > bestDeviceScore) {
					bestDevice = device;
					bestDeviceScore = score;
				}
			}

			return bestDevice;
		}

		private static int VulkanRateDeviceSuitability(VkPhysicalDevice device) { // TODO figure out how i want to rate devices
			int score = 0;

			VkPhysicalDeviceProperties deviceProperties = VkH.GetPhysicalDeviceProperties(device).properties;

			if (deviceProperties.deviceType == VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu) { score += 1000; }
			score += (int)deviceProperties.limits.maxImageDimension2D;

			return score;
		}

		private static unsafe void CreateVulkanDebugMessenger() {
			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = VkH.CreateVkDebugUtilsMessengerCreateInfoEXT();

			VkDebugUtilsMessengerEXT debugMessenger;
			if (Vk.CreateDebugUtilsMessengerEXT(VkInstance!.Value, &messengerCreateInfo, null, &debugMessenger) != VkResult.Success) { // VkInstance shouldn't be null here
				throw new VulkanException("Failed to create Vulkan Debug Messenger");
			}

			Engine3.debugMessenger = debugMessenger;
		}

		private static QueueFamilyIndices VulkanFindQueueFamilies(VkPhysicalDevice device) {
			uint? graphicsFamily = null;

			ReadOnlySpan<VkQueueFamilyProperties2> queueFamilies = VkH.GetPhysicalDeviceQueueFamilyProperties(device);
			uint i = 0;
			foreach (VkQueueFamilyProperties2 queueFamilyProperties2 in queueFamilies) {
				VkQueueFamilyProperties queueFamilyProperties = queueFamilyProperties2.queueFamilyProperties;
				if ((queueFamilyProperties.queueFlags & VkQueueFlagBits.QueueGraphicsBit) != 0) {
					graphicsFamily = i;
					break;
				}

				i++;
			}

			return new(graphicsFamily);
		}

		private static unsafe void VulkanCreateLogicalDevice() {
			QueueFamilyIndices indices = VulkanFindQueueFamilies(VkPhysicalDevice!.Value); // VkPhysicalDevice shouldn't be null here

			float queuePriority = 1f;

			VkDeviceQueueCreateInfo deviceQueueCreateInfo = new() { queueFamilyIndex = indices.GraphicsFamily!.Value, queueCount = 1, pQueuePriorities = &queuePriority, }; // indices.GraphicsFamily shouldn't be null here
			VkPhysicalDeviceFeatures deviceFeatures = new(); // ???

// #if DEBUG
// 			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(VkH.RequiredValidationLayers));
// #endif

			VkDeviceCreateInfo deviceCreateInfo = new() {
					pQueueCreateInfos = &deviceQueueCreateInfo, queueCreateInfoCount = 1, pEnabledFeatures = &deviceFeatures, enabledExtensionCount = 0,
// #if DEBUG // TODO for some reason we're crashing with this enabled. no error message
// 					/* from https://vulkan-tutorial.com/Drawing_a_triangle/Setup/Logical_device_and_queues & https://docs.vulkan.org/spec/latest/appendices/legacy.html#legacy-devicelayers
// 					 * "Previous implementations of Vulkan made a distinction between instance and device specific validation layers, but this is no longer the case.
// 					 * That means that the enabledLayerCount and ppEnabledLayerNames fields of VkDeviceCreateInfo are ignored by up-to-date implementations.
// 					 * However, it is still a good idea to set them anyway to be compatible with older implementations."
// 					 */
// #pragma warning disable CS0618 // Type or member is obsolete
// 					enabledLayerCount = (uint)VkH.RequiredValidationLayers.Count,
// 					ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
// #pragma warning restore CS0618 // Type or member is obsolete
// #else
// 					enabledLayerCount = 0,
// #endif
			};

// #if DEBUG
// 			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, VkH.RequiredValidationLayers.Count);
// #endif

			VkDevice device;
			if (Vk.CreateDevice(VkPhysicalDevice.Value, &deviceCreateInfo, null, &device) != VkResult.Success) { throw new VulkanException("Failed to create logical device"); }
			VkLogicalDevice = device;
		}

		private static unsafe void CleanupVulkan() {
			if (VkInstance == null) { return; }

			if (VkLogicalDevice != null) {
				Vk.DestroyDevice(VkLogicalDevice.Value, null);
				VkLogicalDevice = null;
			}

#if DEBUG
			if (debugMessenger != null) {
				Vk.DestroyDebugUtilsMessengerEXT(VkInstance.Value, debugMessenger.Value, null);
				debugMessenger = null;
			}
#endif

			Vk.DestroyInstance(VkInstance.Value, null);
			VkInstance = null;
		}

		private readonly record struct QueueFamilyIndices { // TODO move?
			public uint? GraphicsFamily { get; init; }

			public QueueFamilyIndices() { }
			public QueueFamilyIndices(uint? graphicsFamily) => GraphicsFamily = graphicsFamily;

			public bool IsValid => GraphicsFamily != null;
		}

		public delegate bool CheckIfDeviceContainsRequiredVulkanFeatures(VkPhysicalDeviceFeatures deviceFeatures); // TODO move?

		public class VulkanSettings {
			public required CheckIfDeviceContainsRequiredVulkanFeatures DeviceVulkanFeatureCheck { get; init; }

			public VulkanSettings() { }

			[SetsRequiredMembers] public VulkanSettings(CheckIfDeviceContainsRequiredVulkanFeatures deviceFeatureCheck) => DeviceVulkanFeatureCheck = deviceFeatureCheck;
		}
	}
}