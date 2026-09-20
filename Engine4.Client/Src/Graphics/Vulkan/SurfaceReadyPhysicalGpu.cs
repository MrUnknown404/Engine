using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public unsafe class SurfaceReadyPhysicalGpu : PhysicalGpu {
	private readonly Surface surface; // TODO use this? or remove it?

	internal QueueFamilyIndices QueueFamilyIndices { get; }

	internal SurfaceReadyPhysicalGpu(PhysicalGpu physicalGpu, Surface surface, QueueFamilyIndices queueFamilyIndices) : base(physicalGpu.VkPhysicalDevice, physicalGpu.PhysicalDeviceProperties2,
		physicalGpu.PhysicalDeviceFeatures2) {
		this.surface = surface;
		QueueFamilyIndices = queueFamilyIndices;
	}

	[MustUseReturnValue]
	public VkFormat FindDepthFormat() =>
			FindSupportedFormat([ VkFormat.FormatD32Sfloat, VkFormat.FormatD32SfloatS8Uint, VkFormat.FormatD24UnormS8Uint, ], VkImageTiling.ImageTilingOptimal,
				VkFormatFeatureFlagBits.FormatFeatureDepthStencilAttachmentBit);

	[MustUseReturnValue]
	public VkFormat FindSupportedFormat(VkFormat[] availableFormats, VkImageTiling tiling, VkFormatFeatureFlagBits featureFlags) {
		foreach (VkFormat format in availableFormats) {
			VkFormatProperties2 formatProperties2 = new();
			Vk.GetPhysicalDeviceFormatProperties2(VkPhysicalDevice, format, &formatProperties2);
			VkFormatProperties formatProperties = formatProperties2.formatProperties;

			switch (tiling) {
				case VkImageTiling.ImageTilingLinear when (formatProperties.linearTilingFeatures & featureFlags) == featureFlags:
				case VkImageTiling.ImageTilingOptimal when (formatProperties.optimalTilingFeatures & featureFlags) == featureFlags: return format;
				case VkImageTiling.ImageTilingDrmFormatModifierExt:
				default: throw new ArgumentOutOfRangeException(nameof(tiling), tiling, null);
			}
		}

		throw new Engine4Exception("Failed to find any supported formats");
	}
}