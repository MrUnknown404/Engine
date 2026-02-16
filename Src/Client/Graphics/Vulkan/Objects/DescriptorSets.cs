using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class DescriptorSets {
		private readonly VkDescriptorSet[] descriptorSets;
		private readonly byte maxFramesInFlight;
		private readonly VkDevice logicalDevice;

		internal DescriptorSets(VkDevice logicalDevice, VkDescriptorSet[] descriptorSets, byte maxFramesInFlight) {
			this.descriptorSets = descriptorSets;
			this.maxFramesInFlight = maxFramesInFlight;
			this.logicalDevice = logicalDevice;
		}

		public VkDescriptorSet GetCurrent(byte frameIndex) => descriptorSets[frameIndex];

		// TODO make UpdateDescriptorSets method

		public void UpdateDescriptorSet(uint binding, DescriptorBuffers descriptorBuffers) {
			VkWriteDescriptorSet* writeDescriptorSets = stackalloc VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorBufferInfo* bufferInfos = stackalloc VkDescriptorBufferInfo[maxFramesInFlight];

			for (byte i = 0; i < maxFramesInFlight; i++) {
				bufferInfos[i] = new() { buffer = descriptorBuffers.GetBuffer(i), range = descriptorBuffers.BufferSize, };
				writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = descriptorBuffers.DescriptorType, descriptorCount = 1, pBufferInfo = &bufferInfos[i], };
			}

			Vk.UpdateDescriptorSets(logicalDevice, maxFramesInFlight, writeDescriptorSets, 0, null);
		}

		public void UpdateDescriptorSet(uint binding, VkImageView imageView, VkSampler textureSampler) {
			VkWriteDescriptorSet* writeDescriptorSets = stackalloc VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorImageInfo imageInfo = new() { imageView = imageView, imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal, sampler = textureSampler, };

			for (int i = 0; i < maxFramesInFlight; i++) {
				writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeCombinedImageSampler, descriptorCount = 1, pImageInfo = &imageInfo, };
			}

			Vk.UpdateDescriptorSets(logicalDevice, maxFramesInFlight, writeDescriptorSets, 0, null);
		}
	}
}