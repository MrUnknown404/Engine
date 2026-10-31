using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

public unsafe class ResourceDescriptors {
	public VulkanBuffer ResourceHeapBuffer { get; }
	public ulong DeviceAddress { get; }
	public ulong MinHeapReservedRange { get; }

	private readonly ulong bufferDescriptorSize;
	private readonly ulong bufferDescriptorHeapOffset;
	private readonly ulong imageDescriptorSize;
	private readonly ulong imageDescriptorHeapOffset;
	private readonly ulong descriptorHeapSize;

	public ResourceDescriptors(VulkanResourceManager resourceManager, SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VulkanTexture[] textures, VulkanBuffer[] buffers) {
		VkPhysicalDeviceDescriptorHeapPropertiesEXT descriptorHeapProperties = physicalGpu.DescriptorHeapProperties;

		ulong count = 1; // TODO what should this be?

		bufferDescriptorSize = AlignSize(descriptorHeapProperties.bufferDescriptorSize, descriptorHeapProperties.bufferDescriptorAlignment);
		imageDescriptorSize = AlignSize(descriptorHeapProperties.imageDescriptorSize, descriptorHeapProperties.imageDescriptorAlignment);
		bufferDescriptorHeapOffset = 0;
		imageDescriptorHeapOffset = AlignSize(bufferDescriptorSize * count, descriptorHeapProperties.imageDescriptorAlignment);

		descriptorHeapSize = AlignSize(imageDescriptorHeapOffset + imageDescriptorSize * count + descriptorHeapProperties.minResourceHeapReservedRange, descriptorHeapProperties.resourceHeapAlignment);

		ResourceHeapBuffer = resourceManager.CreateBuffer($"{nameof(SamplerDescriptors)} Resource Descriptor Heap Buffer", descriptorHeapSize,
			VkBufferUsageFlagBits2.BufferUsage2DescriptorHeapBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		UIntPtr bufferMap = (UIntPtr)ResourceHeapBuffer.MapMemory(Vk.WholeSize); // size?
		DeviceAddress = ResourceHeapBuffer.GetBufferDeviceAddress();
		MinHeapReservedRange = descriptorHeapProperties.minResourceHeapReservedRange;

		VkResourceDescriptorInfoEXT[] descriptorInfos = new VkResourceDescriptorInfoEXT[buffers.Length + textures.Length];
		VkHostAddressRangeEXT[] hostAddressRanges = new VkHostAddressRangeEXT[buffers.Length + textures.Length];

		// buffers
		// VkBuffer[] uniformBuffers; // max frames in flight

		for (int i = 0; i < buffers.Length; i++) { // TODO impl buffers
			// deviceAddressRangesUniformBuffer
			// descriptorInfos
			// hostAddressRanges
		}

		// images
		for (int i = 0; i < textures.Length; i++) {
			VulkanTexture texture = textures[i];

			VkImageViewCreateInfo imageViewCreateInfo = new() {
					image = texture.Image,
					viewType = VkImageViewType.ImageViewType2D,
					format = texture.ImageFormat,
					components = new() {
							r = VkComponentSwizzle.ComponentSwizzleIdentity, //
							g = VkComponentSwizzle.ComponentSwizzleIdentity,
							b = VkComponentSwizzle.ComponentSwizzleIdentity,
							a = VkComponentSwizzle.ComponentSwizzleIdentity,
					},
					subresourceRange = new() {
							aspectMask = texture.ImageAspectMask, //
							baseMipLevel = 0,
							levelCount = 1,
							baseArrayLayer = 0,
							layerCount = 1,
					},
			};

			VkImageDescriptorInfoEXT imageDescriptorInfo = new() {
					pView = &imageViewCreateInfo, //
					layout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal,
			};

			VkResourceDescriptorInfoEXT resourceDescriptorInfo = new() {
					type = VkDescriptorType.DescriptorTypeSampledImage, //
					data = new() { pImage = &imageDescriptorInfo, },
			};

			VkHostAddressRangeEXT hostAddressRange = new() {
					address = (void*)(bufferMap + imageDescriptorHeapOffset), //
					size = (nuint)imageDescriptorSize,
			};

			descriptorInfos[buffers.Length + i] = resourceDescriptorInfo;
			hostAddressRanges[buffers.Length + i] = hostAddressRange;
		}

		fixed (VkResourceDescriptorInfoEXT* pDescriptorInfos = descriptorInfos) {
			fixed (VkHostAddressRangeEXT* pHostAddressRanges = hostAddressRanges) {
				Vk.WriteResourceDescriptorsEXT(logicalGpu.VkLogicalDevice, (uint)descriptorInfos.Length, pDescriptorInfos, pHostAddressRanges); //
			}
		}
	}

	private static ulong AlignSize(ulong value, ulong alignment) => (value + alignment - 1) & ~(alignment - 1);
}