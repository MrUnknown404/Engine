using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class SamplerDescriptors : DescriptorHeap {
	public override VulkanBuffer HeapBuffer { get; }
	public override ulong DeviceAddress { get; }
	public override ulong MinHeapReservedRange { get; }

	public SamplerDescriptors(VulkanResourceManager resourceManager, SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkSamplerCreateInfo[] samplerCreateInfos) : base(resourceManager) {
		VkPhysicalDeviceDescriptorHeapPropertiesEXT descriptorHeapProperties = physicalGpu.DescriptorHeapProperties;
		MinHeapReservedRange = descriptorHeapProperties.minSamplerHeapReservedRange;

		ulong samplerDescriptorSize = AlignSize(descriptorHeapProperties.samplerDescriptorSize, descriptorHeapProperties.samplerDescriptorAlignment);
		ulong descriptorHeapSize = AlignSize(samplerDescriptorSize * (ulong)samplerCreateInfos.Length + descriptorHeapProperties.minSamplerHeapReservedRange, descriptorHeapProperties.samplerHeapAlignment);

		HeapBuffer = resourceManager.CreateBuffer($"{nameof(SamplerDescriptors)} Sampler Descriptor Heap Buffer", descriptorHeapSize,
			VkBufferUsageFlagBits2.BufferUsage2DescriptorHeapBitExt | VkBufferUsageFlagBits2.BufferUsage2ShaderDeviceAddressBit, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit);

		UIntPtr bufferMap = (UIntPtr)HeapBuffer.MapMemory(Vk.WholeSize);
		DeviceAddress = HeapBuffer.GetBufferDeviceAddress();

		VkHostAddressRangeEXT[] samplerHostAddressRange = new VkHostAddressRangeEXT[samplerCreateInfos.Length];
		for (int i = 0; i < samplerCreateInfos.Length; i++) {
			samplerHostAddressRange[i] = new() {
					address = (void*)(bufferMap + samplerDescriptorSize * (ulong)i), //
					size = (nuint)samplerDescriptorSize,
			};
		}

		fixed (VkSamplerCreateInfo* pSamplerCreateInfos = samplerCreateInfos) {
			fixed (VkHostAddressRangeEXT* pSamplerHostAddressRange = samplerHostAddressRange) {
				VkH.ThrowIfNotSuccess(Vk.WriteSamplerDescriptorsEXT(logicalGpu.VkLogicalDevice, (uint)samplerCreateInfos.Length, pSamplerCreateInfos, pSamplerHostAddressRange), "e"); // TODO explain
			}
		}
	}
}