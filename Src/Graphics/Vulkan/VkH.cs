using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;
using ZLinq;

namespace Engine3.Graphics.Vulkan {
	public static unsafe partial class VkH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary> Note: This must be set before you set up Vulkan </summary>
		public static readonly List<string> RequiredValidationLayers = new();
		/// <summary> Note: This must be set before you set up Vulkan </summary>
		public static readonly List<string> RequiredInstanceExtensions = new();
		/// <summary> Note: This must be set before you set up Vulkan </summary>
		public static readonly List<string> RequiredDeviceExtensions = [ Vk.KhrSwapchainExtensionName, ];

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
		/// <param name="version"> Packed integer containing the other 'out' parameters </param>
		/// <param name="variant"> 3-bit integer </param>
		/// <param name="major"> 7-bit integer </param>
		/// <param name="minor"> 10-bit integer </param>
		/// <param name="patch"> 12-bit integer </param>
		/// <seealso href="https://docs.vulkan.org/spec/latest/chapters/extensions.html#extendingvulkan-coreversions">Relevant Vulkan Specification</seealso>
		[Pure]
		public static void GetApiVersion(uint version, out byte variant, out byte major, out ushort minor, out ushort patch) {
			variant = (byte)(version >> 29);
			major = (byte)((version >> 22) & 0x7FU);
			minor = (ushort)((version >> 12) & 0x3FFU);
			patch = (ushort)(version & 0xFFFU);
		}

		[MustUseReturnValue]
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

		[MustUseReturnValue]
		public static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) =>
				Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR vkSurface) != VkResult.Success ? throw new VulkanException("Failed to create surface") : vkSurface;

		[MustUseReturnValue]
		public static Gpu PickBestGpu(Gpu[] gpus, IsGpuSuitable isGpuSuitable, RateGpuSuitability rateGpuSuitability) {
			if (gpus.Length == 0) { throw new VulkanException("Could not find any GPUs"); }

			HashSet<Gpu> capableDevices = new();
			foreach (Gpu gpu in gpus.Where(gpu => isGpuSuitable(gpu))) {
				if (!gpu.QueueFamilyIndices.IsValid) { continue; }
				if (!CheckDeviceExtensionSupport(gpu)) { continue; }

				capableDevices.Add(gpu);
			}

			return SelectBestDevice(capableDevices, rateGpuSuitability) ?? throw new VulkanException("Could not find any suitable GPUs");

			static Gpu? SelectBestDevice(IEnumerable<Gpu> capableGpus, RateGpuSuitability rateGpuSuitability) {
				Gpu? bestDevice = null;
				int bestDeviceScore = 0;

				foreach (Gpu device in capableGpus) {
					int score = rateGpuSuitability(device);
					if (score > bestDeviceScore) {
						bestDevice = device;
						bestDeviceScore = score;
					}
				}

				return bestDevice;
			}

			static bool CheckDeviceExtensionSupport(Gpu gpu) {
				ReadOnlySpan<VkExtensionProperties> availableExtensions = gpu.VkExtensionProperties;
				if (availableExtensions.Length == 0) { throw new VulkanException("Could not find any device extension properties"); }

				foreach (string wantedExtension in RequiredDeviceExtensions) {
					bool layerFound = false;

					foreach (VkExtensionProperties extensionProperties in availableExtensions) {
						ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
						if (Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension) {
							layerFound = true;
							break;
						}
					}

					if (!layerFound) { return false; }
				}

				return true;
			}
		}

		public static void CreateLogicalDevice(Gpu gpu, out VkDevice vkLogicalDevice, out VkQueue vkPresentQueue) {
			QueueFamilyIndices queueFamilyIndices = gpu.QueueFamilyIndices;
			if (queueFamilyIndices.GraphicsFamily == null) { throw new VulkanException("Failed to find GraphicsFamily"); }
			if (queueFamilyIndices.PresentFamily == null) { throw new VulkanException("Failed to find PresentFamily"); }

			HashSet<uint> uniqueQueueFamilies = [ queueFamilyIndices.GraphicsFamily.Value, queueFamilyIndices.PresentFamily.Value, ];
			List<VkDeviceQueueCreateInfo> queueCreateInfos = new();

			float queuePriority = 1f;

			// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator // CS0212
			foreach (uint queueFamily in uniqueQueueFamilies) { queueCreateInfos.Add(new() { queueFamilyIndex = queueFamily, queueCount = 1, pQueuePriorities = &queuePriority, }); }

			VkPhysicalDeviceFeatures deviceFeatures = new(); // ???

			IntPtr requiredDeviceExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(RequiredDeviceExtensions));

#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(RequiredValidationLayers));
#endif

			fixed (VkDeviceQueueCreateInfo* queueCreateInfosPtr = CollectionsMarshal.AsSpan(queueCreateInfos)) {
				VkDeviceCreateInfo deviceCreateInfo = new() {
						pQueueCreateInfos = queueCreateInfosPtr,
						queueCreateInfoCount = (uint)queueCreateInfos.Count,
						pEnabledFeatures = &deviceFeatures,
						ppEnabledExtensionNames = (byte**)requiredDeviceExtensionsPtr,
						enabledExtensionCount = (uint)RequiredDeviceExtensions.Count,
#if DEBUG // https://docs.vulkan.org/spec/latest/appendices/legacy.html#legacy-devicelayers
#pragma warning disable CS0618 // Type or member is obsolete
						enabledLayerCount = (uint)RequiredValidationLayers.Count,
						ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				};

				VkDevice logicalDevice;
				if (Vk.CreateDevice(gpu.VkPhysicalDevice, &deviceCreateInfo, null, &logicalDevice) != VkResult.Success) { throw new VulkanException("Failed to create logical device"); }
				vkLogicalDevice = logicalDevice;
			}

			MarshalTk.FreeStringArrayCoTaskMem(requiredDeviceExtensionsPtr, RequiredDeviceExtensions.Count);

#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, RequiredValidationLayers.Count);
#endif

			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndices.PresentFamily.Value, };

			VkQueue presentQueue;
			Vk.GetDeviceQueue2(vkLogicalDevice, &deviceQueueInfo2, &presentQueue);
			vkPresentQueue = presentQueue;
		}

		[MustUseReturnValue] public static VkPhysicalDevice[] CreatePhysicalDevices(VkInstance vkInstance) => EnumeratePhysicalDevices(vkInstance).ToArray();

		[MustUseReturnValue]
		public static Gpu[] GetValidGpus(VkPhysicalDevice[] devices, VkSurfaceKHR vkSurface) =>
				devices.Select(device => new Gpu(device, GetPhysicalDeviceProperties(device), GetPhysicalDeviceFeatures(device), EnumeratePhysicalDeviceExtensionProperties(device).ToArray(), FindQueueFamilies(device, vkSurface)))
						.ToArray();

		public static void PrintGpus(Gpu[] gpus, bool verbose) {
			if (gpus.Length == 0) { throw new VulkanException("Could not find any GPUs"); }

			Logger.Debug("The following GPUs are available:");
			foreach (Gpu gpu in gpus) {
				if (verbose) {
					foreach (string str in gpu.GetVerboseDescription()) { Logger.Debug(str); }
				} else {
					Logger.Debug($"- {gpu.GetSimpleDescription()}"); //
				}
			}
		}

		[MustUseReturnValue]
		private static bool GetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice vkPhysicalDevice, uint index, VkSurfaceKHR vkSurface) {
			int presentSupport;
			Vk.GetPhysicalDeviceSurfaceSupportKHR(vkPhysicalDevice, index, vkSurface, &presentSupport);
			return presentSupport == 1;
		}

		[MustUseReturnValue]
		private static VkPhysicalDeviceProperties2 GetPhysicalDeviceProperties(VkPhysicalDevice vkPhysicalDevice) {
			VkPhysicalDeviceProperties2 deviceProperties2 = new();
			Vk.GetPhysicalDeviceProperties2(vkPhysicalDevice, &deviceProperties2);
			return deviceProperties2;
		}

		[MustUseReturnValue]
		private static VkPhysicalDeviceFeatures2 GetPhysicalDeviceFeatures(VkPhysicalDevice vkPhysicalDevice) {
			VkPhysicalDeviceFeatures2 deviceFeatures2 = new();
			Vk.GetPhysicalDeviceFeatures2(vkPhysicalDevice, &deviceFeatures2);
			return deviceFeatures2;
		}

		[MustUseReturnValue]
		private static ReadOnlySpan<VkQueueFamilyProperties2> GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice device) {
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

		[MustUseReturnValue]
		private static ReadOnlySpan<VkExtensionProperties> EnumeratePhysicalDeviceExtensionProperties(VkPhysicalDevice vkPhysicalDevice) {
			uint extensionCount;
			Vk.EnumerateDeviceExtensionProperties(vkPhysicalDevice, null, &extensionCount, null);

			if (extensionCount == 0) { return ReadOnlySpan<VkExtensionProperties>.Empty; }

			VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
			fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
				Vk.EnumerateDeviceExtensionProperties(vkPhysicalDevice, null, &extensionCount, extensionPropertiesPtr);
				return extensionProperties;
			}
		}

		[MustUseReturnValue]
		private static ReadOnlySpan<VkPhysicalDevice> EnumeratePhysicalDevices(VkInstance vkInstance) {
			uint deviceCount;
			Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, null);

			if (deviceCount == 0) { return ReadOnlySpan<VkPhysicalDevice>.Empty; }

			VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
			fixed (VkPhysicalDevice* physicalDevicesPtr = physicalDevices) {
				Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, physicalDevicesPtr);
				return physicalDevices;
			}
		}

		[MustUseReturnValue]
		private static QueueFamilyIndices FindQueueFamilies(VkPhysicalDevice vkPhysicalDevice, VkSurfaceKHR vkSurface) {
			uint? graphicsFamily = null;
			uint? presentFamily = null;

			ReadOnlySpan<VkQueueFamilyProperties2> queueFamilies = GetPhysicalDeviceQueueFamilyProperties(vkPhysicalDevice);
			uint i = 0;
			foreach (VkQueueFamilyProperties2 queueFamilyProperties2 in queueFamilies) {
				VkQueueFamilyProperties queueFamilyProperties = queueFamilyProperties2.queueFamilyProperties;
				if ((queueFamilyProperties.queueFlags & VkQueueFlagBits.QueueGraphicsBit) != 0) { graphicsFamily = i; }
				if (GetPhysicalDeviceSurfaceSupportKHR(vkPhysicalDevice, i, vkSurface)) { presentFamily = i; }

				if (graphicsFamily != null && presentFamily != null) { break; } // basically QueueFamilyIndices#IsValid

				i++;
			}

			return new(graphicsFamily, presentFamily);
		}

		public delegate bool IsGpuSuitable(Gpu physicalDevice);
		public delegate int RateGpuSuitability(Gpu physicalDevice);
	}
}