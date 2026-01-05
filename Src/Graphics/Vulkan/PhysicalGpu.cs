using System.Text;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class PhysicalGpu : IEquatable<PhysicalGpu> {
		public VkPhysicalDevice VkPhysicalDevice { get; }
		public VkPhysicalDeviceProperties2 VkPhysicalDeviceProperties2 { get; }
		public VkPhysicalDeviceFeatures2 VkPhysicalDeviceFeatures2 { get; }
		public VkExtensionProperties[] VkExtensionProperties { get; }
		public QueueFamilyIndices QueueFamilyIndices { get; }
		public SwapChainSupportInfo SwapChainSupportInfo { get; }

		public string Name { get; }

		public PhysicalGpu(VkPhysicalDevice vkPhysicalDevice, VkPhysicalDeviceProperties2 vkPhysicalDeviceProperties2, VkPhysicalDeviceFeatures2 vkPhysicalDeviceFeatures2, VkExtensionProperties[] vkExtensionProperties,
			QueueFamilyIndices queueFamilyIndices, SwapChainSupportInfo swapChainSupportInfo) {
			VkPhysicalDevice = vkPhysicalDevice;
			VkPhysicalDeviceProperties2 = vkPhysicalDeviceProperties2;
			VkPhysicalDeviceFeatures2 = vkPhysicalDeviceFeatures2;
			VkExtensionProperties = vkExtensionProperties;
			QueueFamilyIndices = queueFamilyIndices;
			SwapChainSupportInfo = swapChainSupportInfo;

			VkPhysicalDeviceProperties.deviceNameInlineArray1 deviceNameArray = VkPhysicalDeviceProperties2.properties.deviceName;
			ReadOnlySpan<byte> deviceNameSpan = deviceNameArray;
			Name = Encoding.UTF8.GetString(deviceNameSpan[..deviceNameSpan.IndexOf((byte)0)]);
		}

		public string GetSimpleDescription() {
			VkPhysicalDeviceProperties deviceProperties = VkPhysicalDeviceProperties2.properties;
			VkH.GetApiVersion(deviceProperties.apiVersion, out _, out byte major, out ushort minor, out ushort patch);
			return $"{Name} - Api v{deviceProperties.apiVersion.ToString()} ({major}.{minor}.{patch})";
		}

		public string[] GetVerboseDescription() {
			const string PhysicalDeviceTypeEnumName = "PhysicalDeviceType";
			const string VendorIdEnumName = "VendorId";

			VkPhysicalDeviceProperties deviceProperties = VkPhysicalDeviceProperties2.properties;
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

		public bool Equals(PhysicalGpu? other) => other is not null && (other == this || VkPhysicalDevice.Handle.Equals(other.VkPhysicalDevice.Handle));
		public override bool Equals(object? obj) => obj is PhysicalGpu gpu && Equals(gpu);

		public override int GetHashCode() => VkPhysicalDevice.GetHashCode();
	}
}