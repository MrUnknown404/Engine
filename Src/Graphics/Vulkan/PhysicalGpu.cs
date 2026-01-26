using System.Text;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class PhysicalGpu : IEquatable<PhysicalGpu> {
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

			unsafe {
				VkPhysicalDeviceMemoryProperties2 physicalDeviceMemoryProperties2 = new();
				Vk.GetPhysicalDeviceMemoryProperties2(physicalDevice, &physicalDeviceMemoryProperties2);
				PhysicalDeviceMemoryProperties2 = physicalDeviceMemoryProperties2;
			}

			VkPhysicalDeviceProperties.deviceNameInlineArray1 deviceNameArray = PhysicalDeviceProperties2.properties.deviceName;
			ReadOnlySpan<byte> deviceNameSpan = deviceNameArray;
			Name = Encoding.UTF8.GetString(deviceNameSpan[..deviceNameSpan.IndexOf((byte)0)]);
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
			featuresStringBuilder.Append($"SamplerAnisotropy: {features.samplerAnisotropy}");

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
		}

		public static bool operator ==(PhysicalGpu? left, PhysicalGpu? right) => Equals(left, right);
		public static bool operator !=(PhysicalGpu? left, PhysicalGpu? right) => !Equals(left, right);

		public bool Equals(PhysicalGpu? other) => other is not null && (other == this || PhysicalDevice.Handle.Equals(other.PhysicalDevice.Handle));
		public override bool Equals(object? obj) => obj is PhysicalGpu gpu && Equals(gpu);

		public override int GetHashCode() => PhysicalDevice.GetHashCode();
	}
}