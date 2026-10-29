using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

// https://docs.vulkan.org/guide/latest/descriptor_heap.html
// https://docs.vulkan.org/spec/latest/chapters/descriptorheaps.html
// https://github.com/KhronosGroup/Vulkan-Samples/blob/main/samples/extensions/descriptor_heap/descriptor_heap.cpp
// https://github.com/SaschaWillems/Vulkan/blob/master/examples/descriptorheap/descriptorheap.cpp

public unsafe class SamplerDescriptors {
	public VulkanBuffer SamplerHeapBuffer { get; }

	private readonly ulong descriptorSize;
	private readonly ulong descriptorHeapOffset;
	private readonly ulong descriptorHeapSize;

	public SamplerDescriptors(VulkanResourceManager resourceManager, SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkSamplerCreateInfo[] samplerCreateInfos) {
		VkPhysicalDeviceDescriptorHeapPropertiesEXT descriptorHeapProperties = physicalGpu.DescriptorHeapProperties;
		descriptorSize = AlignSize(descriptorHeapProperties.samplerDescriptorSize, descriptorHeapProperties.samplerDescriptorAlignment);
		descriptorHeapOffset = 0;
		descriptorHeapSize = AlignSize((ulong)samplerCreateInfos.Length * descriptorSize + descriptorHeapProperties.minSamplerHeapReservedRange, descriptorHeapProperties.samplerHeapAlignment);

		SamplerHeapBuffer = resourceManager.CreateBuffer($"{nameof(SamplerDescriptors)} Sampler Descriptor Heap Buffer", descriptorHeapSize,
			VkBufferUsageFlagBits2.BufferUsage2SamplerDescriptorBufferBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		UIntPtr bufferMap = (UIntPtr)SamplerHeapBuffer.MapMemory(Vk.WholeSize); // size?

		VkHostAddressRangeEXT[] samplerHostAddressRange = new VkHostAddressRangeEXT[samplerCreateInfos.Length];
		for (int i = 0; i < samplerCreateInfos.Length; i++) {
			samplerHostAddressRange[i] = new() {
					address = (void*)(bufferMap + descriptorHeapOffset + descriptorSize * (ulong)i), //
					size = (nuint)descriptorSize,
			};
		}

		fixed (VkSamplerCreateInfo* pSamplerCreateInfos = samplerCreateInfos) {
			fixed (VkHostAddressRangeEXT* pSamplerHostAddressRange = samplerHostAddressRange) {
				Vk.WriteSamplerDescriptorsEXT(logicalGpu.VkLogicalDevice, (uint)samplerCreateInfos.Length, pSamplerCreateInfos, pSamplerHostAddressRange);
			}
		}
	}

	private static ulong AlignSize(ulong value, ulong alignment) => (value + alignment - 1) & ~(alignment - 1);
}

// public unsafe class ResourceDescriptors {
// 	public VulkanBuffer ResourceHeapBuffer { get; }
//
// 	private readonly ulong bufferDescriptorSize;
// 	private readonly ulong bufferDescriptorHeapOffset;
// 	private readonly ulong imageDescriptorSize;
// 	private readonly ulong imageDescriptorHeapOffset;
// 	private readonly ulong descriptorHeapSize;
//
// 	public ResourceDescriptors(VulkanResourceManager resourceManager, SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu) {
// 		VkPhysicalDeviceDescriptorHeapPropertiesEXT descriptorHeapProperties = physicalGpu.DescriptorHeapProperties;
// 		bufferDescriptorSize = AlignSize(descriptorHeapProperties.bufferDescriptorSize, descriptorHeapProperties.bufferDescriptorAlignment);
// 		imageDescriptorSize = AlignSize(descriptorHeapProperties.imageDescriptorSize, descriptorHeapProperties.imageDescriptorAlignment);
// 		bufferDescriptorHeapOffset = 0;
// 		imageDescriptorHeapOffset = AlignSize(bufferDescriptorSize * count, descriptorHeapProperties.imageDescriptorAlignment);
//
// 		ResourceHeapBuffer = resourceManager.CreateBuffer($"{nameof(SamplerDescriptors)} Resource Descriptor Heap Buffer", descriptorHeapSize,
// 			VkBufferUsageFlagBits2.BufferUsage2ResourceDescriptorBufferBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);
// 	}
//
// 	private static ulong AlignSize(ulong value, ulong alignment) => (value + alignment - 1) & ~(alignment - 1);
// }