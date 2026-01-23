using System.Text;
using Engine3.Api.Graphics;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class PhysicalGpu : IEquatable<PhysicalGpu> {
		public VkPhysicalDevice PhysicalDevice { get; }
		public VkPhysicalDeviceProperties2 PhysicalDeviceProperties2 { get; }
		public VkPhysicalDeviceFeatures2 PhysicalDeviceFeatures2 { get; }
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
			int physicalDeviceTypeEnumNameLength = PhysicalDeviceTypeEnumName.Length;
			int vendorIdEnumNameLength = VendorIdEnumName.Length;

			VkH.GetApiVersion(deviceProperties.apiVersion, out _, out byte major, out ushort minor, out ushort patch);

			//@formatter:off
			return [
					"Gpu: [",
					"- VkPhysicalDevice: [",
					$"- - Device Type: {deviceProperties.deviceType.ToString()[physicalDeviceTypeEnumNameLength..]}",
					$"- - Device Api Version: {deviceProperties.apiVersion.ToString()} ({major}.{minor}.{patch})",
					$"- - Device Id: {deviceProperties.deviceID.ToString()}",
					$"- - Device Name: {Name}",
					$"- - Vendor Driver Version: {deviceProperties.driverVersion.ToString()}",
					$"- - Vendor Id: {deviceProperties.vendorID}{(deviceProperties.vendorID is >= (uint)VkVendorId.VendorIdKhronos and <= (uint)VkVendorId.VendorIdMobileye ? $", ({((VkVendorId)deviceProperties.vendorID).ToString()[vendorIdEnumNameLength..]})" : string.Empty)}",
					"- ]", // TODO add more
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