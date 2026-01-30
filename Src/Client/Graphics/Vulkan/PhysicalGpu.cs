using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public unsafe class PhysicalGpu : IEquatable<PhysicalGpu> {
		public VkPhysicalDevice PhysicalDevice { get; }
		public VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; }
		public VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; }
		public VkPhysicalDeviceMemoryProperties2 PhysicalDeviceMemoryProperties2 { get; }
		public VkExtensionProperties[] ExtensionProperties { get; }
		public QueueFamilyIndices QueueFamilyIndices { get; }

		public string Name { get; }

		public PhysicalGpu(VkPhysicalDevice physicalDevice, VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2, VkExtensionProperties[] extensionProperties,
			QueueFamilyIndices queueFamilyIndices) {
			PhysicalDevice = physicalDevice;
			PhysicalDeviceProperties2 = physicalDeviceProperties2;
			PhysicalDeviceFeatures2 = physicalDeviceFeatures2;
			ExtensionProperties = extensionProperties;
			QueueFamilyIndices = queueFamilyIndices;

			VkPhysicalDeviceMemoryProperties2 physicalDeviceMemoryProperties2 = new();
			Vk.GetPhysicalDeviceMemoryProperties2(physicalDevice, &physicalDeviceMemoryProperties2);
			PhysicalDeviceMemoryProperties2 = physicalDeviceMemoryProperties2;

			VkPhysicalDeviceProperties.deviceNameInlineArray1 deviceNameArray = PhysicalDeviceProperties2.properties.deviceName;
			ReadOnlySpan<byte> deviceNameSpan = deviceNameArray;
			Name = Encoding.UTF8.GetString(deviceNameSpan[..deviceNameSpan.IndexOf((byte)0)]);
		}

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

		public string GetSimpleDescription() {
			VkPhysicalDeviceProperties deviceProperties = PhysicalDeviceProperties2.properties;
			VkH.GetApiVersion(deviceProperties.apiVersion, out _, out byte major, out ushort minor, out ushort patch);
			return $"{Name} - Api v{deviceProperties.apiVersion.ToString()} ({major}.{minor}.{patch})";
		}

		public string[] GetVerboseDescription() {
			const string PhysicalDeviceTypeEnumName = "PhysicalDeviceType";
			const string VendorIdEnumName = "VendorId";

			VkPhysicalDeviceProperties deviceProperties = PhysicalDeviceProperties2.properties;
			VkH.GetApiVersion(deviceProperties.apiVersion, out _, out byte major, out ushort minor, out ushort patch);

			uint vendorId = deviceProperties.vendorID;
			string vendorIdName = (VkVendorId)vendorId is >= VkVendorId.VendorIdKhronos and <= VkVendorId.VendorIdMobileye ? $", ({((VkVendorId)vendorId).ToString()[VendorIdEnumName.Length..]})" : string.Empty;

			VkPhysicalDeviceLimits limits = deviceProperties.limits;
			VkPhysicalDeviceFeatures features = PhysicalDeviceFeatures2.features;

			StringBuilder limitsStringBuilder = new();
			limitsStringBuilder.Append($"MaxSamplerAnisotropy: {limits.maxSamplerAnisotropy}");

			StringBuilder featuresStringBuilder = new();
			featuresStringBuilder.Append($"SamplerAnisotropy: {VkBoolToBool(features.samplerAnisotropy)}");

			// TODO PhysicalDeviceMemoryProperties2
			// TODO ExtensionProperties

			//@formatter:off
			return [
				"VkPhysicalDevice: [",
				"- Properties: [",
				$"- - Type / Name: {deviceProperties.deviceType.ToString()[PhysicalDeviceTypeEnumName.Length..]} / {Name}",
				$"- - Api / Driver Version: {deviceProperties.apiVersion} ({major}.{minor}.{patch}) / {deviceProperties.driverVersion}",
				$"- - Device / Vendor Id: {deviceProperties.deviceID} / {vendorId}{vendorIdName}",
				"- - Limits: [",
				$"- - - {limitsStringBuilder}",
				"- - ]",
				"- ],",
				"- Features: [",
				$"- - - {featuresStringBuilder}",
				"- ]",
				"]",
			];
			//@formatter:on

			static bool VkBoolToBool(int vkBool) => vkBool == Vk.True;
		}

		public static bool operator ==(PhysicalGpu? left, PhysicalGpu? right) => Equals(left, right);
		public static bool operator !=(PhysicalGpu? left, PhysicalGpu? right) => !Equals(left, right);

		public bool Equals(PhysicalGpu? other) => other is not null && (other == this || PhysicalDevice.Handle.Equals(other.PhysicalDevice.Handle));
		public override bool Equals(object? obj) => obj is PhysicalGpu gpu && Equals(gpu);

		public override int GetHashCode() => PhysicalDevice.GetHashCode();
	}
}