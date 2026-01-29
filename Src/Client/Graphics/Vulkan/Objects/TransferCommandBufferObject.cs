using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class TransferCommandBufferObject : CommandBufferObject {
		public TransferCommandBufferObject(VkDevice logicalDevice, VkCommandPool commandPool, VkQueue queue, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) : base(logicalDevice, commandPool,
			CreateCommandBuffer(logicalDevice, commandPool, level), queue) { }

		public void CmdCopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong bufferSize) {
			VkBufferCopy2 bufferCopy2 = new() { size = bufferSize, };
			VkCopyBufferInfo2 copyBufferInfo2 = new() { srcBuffer = srcBuffer, dstBuffer = dstBuffer, regionCount = 1, pRegions = &bufferCopy2, };
			Vk.CmdCopyBuffer2(CommandBuffer, &copyBufferInfo2);
		}

		public void CmdCopyImage(VkBuffer srcBuffer, VkImage dstImage, uint width, uint height) {
			VkBufferImageCopy2 copyImageInfo2 = new() {
					bufferOffset = 0,
					bufferRowLength = 0,
					bufferImageHeight = 0,
					imageSubresource = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, mipLevel = 0, baseArrayLayer = 0, layerCount = 1, },
					imageOffset = new(0, 0, 0),
					imageExtent = new(width, height, 1),
			};

			VkCopyBufferToImageInfo2 copyBufferToImageInfo2 = new() { srcBuffer = srcBuffer, dstImage = dstImage, dstImageLayout = VkImageLayout.ImageLayoutTransferDstOptimal, regionCount = 1, pRegions = &copyImageInfo2, };
			Vk.CmdCopyBufferToImage2(CommandBuffer, &copyBufferToImageInfo2);
		}

		public void TransitionImageLayout(QueueFamilyIndices queueFamilyIndices, VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout) {
			VkImageMemoryBarrier2 imageMemoryBarrier = VkH.CreateImageBarrier(queueFamilyIndices.GraphicsFamily, queueFamilyIndices.TransferFamily, image, format, oldLayout, newLayout);
			CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, });
		}
	}
}