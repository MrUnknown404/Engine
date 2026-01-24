using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class TransferCommandBuffer : CommandBuffer {
		public TransferCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) : base(logicalDevice, commandPool,
			CreateCommandBuffer(logicalDevice, commandPool, level)) { }

		public void CmdCopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong bufferSize) {
			VkBufferCopy2 bufferCopy2 = new() { size = bufferSize, };
			VkCopyBufferInfo2 copyBufferInfo2 = new() { srcBuffer = srcBuffer, dstBuffer = dstBuffer, regionCount = 1, pRegions = &bufferCopy2, };
			Vk.CmdCopyBuffer2(VkCommandBuffer, &copyBufferInfo2);
		}

		public void SubmitQueue(VkQueue transferQueue) {
			VkCommandBuffer commandBuffer = VkCommandBuffer;
			VkSubmitInfo submitInfo = new() { commandBufferCount = 1, pCommandBuffers = &commandBuffer, };
			VkH.SubmitQueue(transferQueue, submitInfo, VkFence.Zero);
		}
	}
}