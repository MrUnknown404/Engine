using System.Runtime.InteropServices;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public unsafe class SurfaceCapablePhysicalGpu : PhysicalGpu {
		public QueueFamilyIndices QueueFamilyIndices { get; }

		internal SurfaceCapablePhysicalGpu(PhysicalGpu physicalGpu, QueueFamilyIndices queueFamilyIndices) : base(physicalGpu.PhysicalDevice, physicalGpu.PhysicalDeviceProperties2, physicalGpu.PhysicalDeviceFeatures2,
			physicalGpu.ExtensionProperties) =>
				QueueFamilyIndices = queueFamilyIndices;

		public LogicalGpu CreateLogicalDevice(string[] requiredDeviceExtensions, string[] requiredValidationLayers) {
			List<VkDeviceQueueCreateInfo> queueCreateInfos = new();
			float queuePriority = 1f;

			// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator // CS0212
			foreach (uint queueFamily in new HashSet<uint> { QueueFamilyIndices.GraphicsFamily, QueueFamilyIndices.PresentFamily, QueueFamilyIndices.TransferFamily, }) {
				queueCreateInfos.Add(new() { queueFamilyIndex = queueFamily, queueCount = 1, pQueuePriorities = &queuePriority, });
			}

			VkPhysicalDeviceVulkan11Features physicalDeviceVulkan11Features = new(); // atm i'm not using most of these. but i may?
			VkPhysicalDeviceVulkan12Features physicalDeviceVulkan12Features = new() { pNext = &physicalDeviceVulkan11Features, };
			VkPhysicalDeviceVulkan13Features physicalDeviceVulkan13Features = new() { pNext = &physicalDeviceVulkan12Features, synchronization2 = (int)Vk.True, dynamicRendering = (int)Vk.True, };
			VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = new() { pNext = &physicalDeviceVulkan13Features, };
			VkPhysicalDeviceFeatures physicalDeviceFeatures = new() { samplerAnisotropy = (int)Vk.True, };

			IntPtr requiredDeviceExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredDeviceExtensions);
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredValidationLayers);
#endif

			fixed (VkDeviceQueueCreateInfo* queueCreateInfosPtr = CollectionsMarshal.AsSpan(queueCreateInfos)) {
				VkDeviceCreateInfo deviceCreateInfo = new() {
						pNext = &physicalDeviceVulkan14Features,
						pQueueCreateInfos = queueCreateInfosPtr,
						queueCreateInfoCount = (uint)queueCreateInfos.Count,
						pEnabledFeatures = &physicalDeviceFeatures,
						ppEnabledExtensionNames = (byte**)requiredDeviceExtensionsPtr,
						enabledExtensionCount = (uint)requiredDeviceExtensions.Length,
#if DEBUG // https://docs.vulkan.org/spec/latest/appendices/legacy.html#legacy-devicelayers
#pragma warning disable CS0618 // Type or member is obsolete
						enabledLayerCount = (uint)requiredValidationLayers.Length,
						ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				};

				VkDevice logicalDevice;
				VkResult result = Vk.CreateDevice(PhysicalDevice, &deviceCreateInfo, null, &logicalDevice);

				MarshalTk.FreeStringArrayCoTaskMem(requiredDeviceExtensionsPtr, requiredDeviceExtensions.Length);
#if DEBUG
				MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

				VkH.CheckIfSuccess(result, VulkanException.Reason.CreateLogicalDevice);

				VkQueue graphicsQueue = GetDeviceQueue(logicalDevice, QueueFamilyIndices.GraphicsFamily);
				VkQueue presentQueue = GetDeviceQueue(logicalDevice, QueueFamilyIndices.PresentFamily);
				VkQueue transferQueue = GetDeviceQueue(logicalDevice, QueueFamilyIndices.TransferFamily);

				return new(this, logicalDevice, graphicsQueue, presentQueue, transferQueue);

				[MustUseReturnValue]
				static VkQueue GetDeviceQueue(VkDevice logicalDevice, uint queueFamilyIndex) {
					VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
					VkQueue queue;
					Vk.GetDeviceQueue2(logicalDevice, &deviceQueueInfo2, &queue);
					return queue;
				}
			}
		}

		[MustUseReturnValue]
		public VkFormat FindDepthFormat() =>
				FindSupportedFormat([ VkFormat.FormatD32Sfloat, VkFormat.FormatD32SfloatS8Uint, VkFormat.FormatD24UnormS8Uint, ], VkImageTiling.ImageTilingOptimal, VkFormatFeatureFlagBits.FormatFeatureDepthStencilAttachmentBit);

		[MustUseReturnValue]
		public VkFormat FindSupportedFormat(VkFormat[] availableFormats, VkImageTiling tiling, VkFormatFeatureFlagBits featureFlags) {
			foreach (VkFormat format in availableFormats) {
				VkFormatProperties formatProperties = new();
				Vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, format, &formatProperties); // TODO 2

				switch (tiling) {
					case VkImageTiling.ImageTilingLinear when (formatProperties.linearTilingFeatures & featureFlags) == featureFlags:
					case VkImageTiling.ImageTilingOptimal when (formatProperties.optimalTilingFeatures & featureFlags) == featureFlags: return format;
					case VkImageTiling.ImageTilingDrmFormatModifierExt:
					default: throw new ArgumentOutOfRangeException(nameof(tiling), tiling, null);
				}
			}

			throw new Engine3VulkanException("Failed to find any supported formats");
		}
	}
}