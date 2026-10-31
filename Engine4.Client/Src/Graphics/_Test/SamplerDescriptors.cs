using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics._Test;

// https://docs.vulkan.org/guide/latest/descriptor_heap.html
// https://docs.vulkan.org/spec/latest/chapters/descriptorheaps.html
// https://github.com/KhronosGroup/Vulkan-Samples/blob/main/samples/extensions/descriptor_heap/README.adoc
// https://github.com/KhronosGroup/Vulkan-Samples/blob/main/samples/extensions/descriptor_heap/descriptor_heap.cpp
// https://github.com/SaschaWillems/Vulkan/blob/master/examples/descriptorheap/descriptorheap.cpp

public unsafe class SamplerDescriptors {
	public VulkanBuffer SamplerHeapBuffer { get; }
	public ulong DeviceAddress { get; }
	public ulong MinHeapReservedRange { get; }

	private readonly ulong samplerDescriptorSize;
	private readonly ulong samplerDescriptorHeapOffset;
	private readonly ulong descriptorHeapSize;

	public SamplerDescriptors(VulkanResourceManager resourceManager, SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkSamplerCreateInfo[] samplerCreateInfos) {
		VkPhysicalDeviceDescriptorHeapPropertiesEXT descriptorHeapProperties = physicalGpu.DescriptorHeapProperties;
		samplerDescriptorSize = AlignSize(descriptorHeapProperties.samplerDescriptorSize, descriptorHeapProperties.samplerDescriptorAlignment);
		samplerDescriptorHeapOffset = 0;
		descriptorHeapSize = AlignSize(samplerDescriptorSize * (ulong)samplerCreateInfos.Length + descriptorHeapProperties.minSamplerHeapReservedRange, descriptorHeapProperties.samplerHeapAlignment);

		SamplerHeapBuffer = resourceManager.CreateBuffer($"{nameof(SamplerDescriptors)} Sampler Descriptor Heap Buffer", descriptorHeapSize,
			VkBufferUsageFlagBits2.BufferUsage2DescriptorHeapBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		UIntPtr bufferMap = (UIntPtr)SamplerHeapBuffer.MapMemory(Vk.WholeSize); // size?
		DeviceAddress = SamplerHeapBuffer.GetBufferDeviceAddress();
		MinHeapReservedRange = descriptorHeapProperties.minSamplerHeapReservedRange;

		VkHostAddressRangeEXT[] samplerHostAddressRange = new VkHostAddressRangeEXT[samplerCreateInfos.Length];
		for (int i = 0; i < samplerCreateInfos.Length; i++) {
			samplerHostAddressRange[i] = new() {
					address = (void*)(bufferMap + samplerDescriptorHeapOffset + samplerDescriptorSize * (ulong)i), //
					size = (nuint)samplerDescriptorSize,
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