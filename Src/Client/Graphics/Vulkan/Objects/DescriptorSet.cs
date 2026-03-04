using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class DescriptorSet { // just kinda nice for static data
		public VkDescriptorSet VkDescriptorSet { get; }

		private readonly LogicalGpu logicalGpu;

		internal DescriptorSet(LogicalGpu logicalGpu, VkDescriptorSet vkDescriptorSet) {
			VkDescriptorSet = vkDescriptorSet;
			this.logicalGpu = logicalGpu;
		}

		public void UpdateDescriptorSet(uint binding, DescriptorBuffer descriptorBuffer) {
			VkDescriptorBufferInfo bufferInfo = new() { buffer = descriptorBuffer.Buffer.Buffer, range = descriptorBuffer.Buffer.BufferSize, };
			VkWriteDescriptorSet writeDescriptorSet = new() { dstBinding = binding, dstSet = VkDescriptorSet, descriptorType = descriptorBuffer.DescriptorType, descriptorCount = 1, pBufferInfo = &bufferInfo, };

			Vk.UpdateDescriptorSets(logicalGpu.LogicalDevice, 1, &writeDescriptorSet, 0, null);
		}
	}
}