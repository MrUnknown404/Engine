using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class ResourceDescriptors : DescriptorHeap {
	public override VulkanBuffer HeapBuffer { get; }
	public override ulong DeviceAddress { get; }
	public override ulong MinHeapReservedRange { get; }

	public ResourceDescriptors(VulkanResourceManager resourceManager, SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VulkanTexture[] textures, VulkanBuffer[] buffers) : base(resourceManager) {
		VkPhysicalDeviceDescriptorHeapPropertiesEXT descriptorHeapProperties = physicalGpu.DescriptorHeapProperties;
		MinHeapReservedRange = descriptorHeapProperties.minResourceHeapReservedRange;

		ulong bufferDescriptorSize = AlignSize(descriptorHeapProperties.bufferDescriptorSize, descriptorHeapProperties.bufferDescriptorAlignment);
		ulong imageDescriptorSize = AlignSize(descriptorHeapProperties.imageDescriptorSize, descriptorHeapProperties.imageDescriptorAlignment);
		ulong imageDescriptorHeapOffset = AlignSize(bufferDescriptorSize * (ulong)buffers.Length, descriptorHeapProperties.imageDescriptorAlignment);

		ulong descriptorHeapSize = AlignSize(imageDescriptorHeapOffset + imageDescriptorSize * (ulong)textures.Length + descriptorHeapProperties.minResourceHeapReservedRange,
			descriptorHeapProperties.resourceHeapAlignment);

		HeapBuffer = resourceManager.CreateBuffer($"{nameof(SamplerDescriptors)} Resource Descriptor Heap Buffer", descriptorHeapSize,
			VkBufferUsageFlagBits2.BufferUsage2DescriptorHeapBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		UIntPtr bufferMap = (UIntPtr)HeapBuffer.MapMemory(Vk.WholeSize);
		DeviceAddress = HeapBuffer.GetBufferDeviceAddress();

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
				VkH.ThrowIfNotSuccess(Vk.WriteResourceDescriptorsEXT(logicalGpu.VkLogicalDevice, (uint)descriptorInfos.Length, pDescriptorInfos, pHostAddressRanges), "e"); // TODO explain
			}
		}
	}
}