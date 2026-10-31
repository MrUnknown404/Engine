using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public unsafe class SurfaceReadyPhysicalGpu : PhysicalGpu {
	public VkPhysicalDeviceMemoryProperties2 MemoryProperties2 { get; }
	public VkPhysicalDeviceDescriptorHeapPropertiesEXT DescriptorHeapProperties { get; }
	public QueueFamilyIndices QueueFamilyIndices { get; }
	public VkFormat DepthFormat { get; }

	private readonly Surface surface; // TODO use this? or remove it?
	private readonly Dictionary<VkDescriptorType, ulong> descriptorTypeSizes = new();

	internal SurfaceReadyPhysicalGpu(PhysicalGpu physicalGpu, Surface surface, QueueFamilyIndices queueFamilyIndices) : base(physicalGpu.VkPhysicalDevice, physicalGpu.Properties2, physicalGpu.Features2) {
		MemoryProperties2 = GetMemoryProperties(physicalGpu);
		DescriptorHeapProperties = GetDescriptorHeapProperties(physicalGpu);
		QueueFamilyIndices = queueFamilyIndices;
		DepthFormat = FindSupportedFormat([ VkFormat.FormatD32Sfloat, VkFormat.FormatD32SfloatS8Uint, VkFormat.FormatD24UnormS8Uint, ], VkImageTiling.ImageTilingOptimal,
			VkFormatFeatureFlagBits.FormatFeatureDepthStencilAttachmentBit);

		this.surface = surface;

		// https://docs.vulkan.org/refpages/latest/refpages/source/vkGetPhysicalDeviceDescriptorSizeEXT.html#VUID-vkGetPhysicalDeviceDescriptorSizeEXT-type-11362
		AddDescriptorType(VkDescriptorType.DescriptorTypeSampler);
		AddDescriptorType(VkDescriptorType.DescriptorTypeUniformBuffer);
		AddDescriptorType(VkDescriptorType.DescriptorTypeStorageBuffer);
		AddDescriptorType(VkDescriptorType.DescriptorTypeSampledImage);
		AddDescriptorType(VkDescriptorType.DescriptorTypeStorageImage);
		AddDescriptorType(VkDescriptorType.DescriptorTypeUniformTexelBuffer);
		AddDescriptorType(VkDescriptorType.DescriptorTypeStorageTexelBuffer);
		AddDescriptorType(VkDescriptorType.DescriptorTypeSampleWeightImageQcom);
		AddDescriptorType(VkDescriptorType.DescriptorTypeBlockMatchImageQcom);
		AddDescriptorType(VkDescriptorType.DescriptorTypeAccelerationStructureKhr);
		AddDescriptorType(VkDescriptorType.DescriptorTypeAccelerationStructureNv);
		AddDescriptorType(VkDescriptorType.DescriptorTypePartitionedAccelerationStructureNv);
		AddDescriptorType(VkDescriptorType.DescriptorTypeTensorArm);
		AddDescriptorType(VkDescriptorType.DescriptorTypeInputAttachment);

		return;

		void AddDescriptorType(VkDescriptorType descriptorType) => descriptorTypeSizes[descriptorType] = Vk.GetPhysicalDeviceDescriptorSizeEXT(VkPhysicalDevice, descriptorType);
	}

	[MustUseReturnValue]
	public ulong GetDescriptorSize(VkDescriptorType descriptorType) => descriptorTypeSizes[descriptorType];

	[MustUseReturnValue]
	private VkFormat FindSupportedFormat(VkFormat[] wantedFormats, VkImageTiling tiling, VkFormatFeatureFlagBits featureFlags) {
		foreach (VkFormat format in wantedFormats) {
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

	[MustUseReturnValue]
	private static VkPhysicalDeviceMemoryProperties2 GetMemoryProperties(PhysicalGpu physicalGpu) {
		VkPhysicalDeviceMemoryProperties2 physicalDeviceMemoryProperties2 = new();
		Vk.GetPhysicalDeviceMemoryProperties2(physicalGpu.VkPhysicalDevice, &physicalDeviceMemoryProperties2);
		return physicalDeviceMemoryProperties2;
	}

	[MustUseReturnValue]
	private static VkPhysicalDeviceDescriptorHeapPropertiesEXT GetDescriptorHeapProperties(PhysicalGpu physicalGpu) {
		VkPhysicalDeviceDescriptorHeapPropertiesEXT physicalDeviceDescriptorHeapProperties = new();
		VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new() { pNext = &physicalDeviceDescriptorHeapProperties, };
		Vk.GetPhysicalDeviceProperties2(physicalGpu.VkPhysicalDevice, &physicalDeviceProperties2);
		return physicalDeviceDescriptorHeapProperties;
	}
}