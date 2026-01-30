using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class DescriptorSets {
		private readonly VkDescriptorSet[] descriptorSets;
		private readonly byte maxFramesInFlight;
		private readonly VkDevice logicalDevice;

		internal DescriptorSets(VkDevice logicalDevice, VkDescriptorPool descriptorPool, VkDescriptorSetLayout descriptorSetLayout, byte maxFramesInFlight) {
			descriptorSets = VkH.AllocateDescriptorSets(logicalDevice, descriptorPool, descriptorSetLayout, maxFramesInFlight);
			this.maxFramesInFlight = maxFramesInFlight;
			this.logicalDevice = logicalDevice;
		}

		public VkDescriptorSet GetCurrent(byte frameIndex) => descriptorSets[frameIndex];

		public void UpdateDescriptorSet(uint binding, UniformBuffers uniformBuffers, ulong range, ulong offset = 0) {
			VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorBufferInfo[] bufferInfos = new VkDescriptorBufferInfo[maxFramesInFlight];

			fixed (VkDescriptorBufferInfo* bufferInfosPtr = bufferInfos) {
				for (byte i = 0; i < maxFramesInFlight; i++) {
					bufferInfosPtr[i] = new() { buffer = uniformBuffers.GetBuffer(i), offset = offset, range = range, };
					writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, pBufferInfo = &bufferInfosPtr[i], };
				}

				fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
			}
		}

		public void UpdateDescriptorSet(uint binding, VkImageView imageView, VkSampler textureSampler) {
			VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorImageInfo imageInfo = new() { imageView = imageView, imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal, sampler = textureSampler, };

			for (int i = 0; i < maxFramesInFlight; i++) {
				writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeCombinedImageSampler, descriptorCount = 1, pImageInfo = &imageInfo, };
			}

			fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
		}
	}
}