using System.Diagnostics.CodeAnalysis;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class TransferCommandBuffer : CommandBuffer {
		internal TransferCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) : base(logicalDevice, commandPool,
			CreateCommandBuffer(logicalDevice, commandPool, level)) { }

		public void CmdCopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong bufferSize) {
			VkBufferCopy2 bufferCopy2 = new() { size = bufferSize, };
			VkCopyBufferInfo2 copyBufferInfo2 = new() { srcBuffer = srcBuffer, dstBuffer = dstBuffer, regionCount = 1, pRegions = &bufferCopy2, };
			Vk.CmdCopyBuffer2(VkCommandBuffer, &copyBufferInfo2);
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
			Vk.CmdCopyBufferToImage2(VkCommandBuffer, &copyBufferToImageInfo2);
		}

		public void TransitionImageLayout(QueueFamilyIndices queueFamilyIndices, VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout) {
			VkImageMemoryBarrier2 imageMemoryBarrier = CreateImageBarrier(queueFamilyIndices.GraphicsFamily, queueFamilyIndices.TransferFamily, image, format, oldLayout, newLayout);
			CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, });

			return;

			[SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
			[MustUseReturnValue]
			static VkImageMemoryBarrier2 CreateImageBarrier(uint graphicsFamily, uint transferFamily, VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout) {
				VkAccessFlagBits2 srcAccessMask;
				VkAccessFlagBits2 dstAccessMask;
				VkPipelineStageFlagBits2 srcStageMask;
				VkPipelineStageFlagBits2 dstStageMask;
				VkImageAspectFlagBits aspectMask;

				if (newLayout == VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal) {
					aspectMask = VkImageAspectFlagBits.ImageAspectDepthBit;

					if (format is VkFormat.FormatD32SfloatS8Uint or VkFormat.FormatD24UnormS8Uint) { aspectMask |= VkImageAspectFlagBits.ImageAspectStencilBit; }
				} else { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit; }

				switch (oldLayout) {
					case VkImageLayout.ImageLayoutUndefined when newLayout == VkImageLayout.ImageLayoutTransferDstOptimal:
						srcAccessMask = 0;
						dstAccessMask = VkAccessFlagBits2.Access2TransferWriteBit;
						srcStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit;
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TransferBit;
						break;
					case VkImageLayout.ImageLayoutUndefined when newLayout == VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal:
						srcAccessMask = 0;
						dstAccessMask = VkAccessFlagBits2.Access2DepthStencilAttachmentReadBit | VkAccessFlagBits2.Access2DepthStencilAttachmentWriteBit;
						srcStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit;
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2EarlyFragmentTestsBit;
						break;
					case VkImageLayout.ImageLayoutTransferDstOptimal when newLayout == VkImageLayout.ImageLayoutShaderReadOnlyOptimal:
						srcAccessMask = VkAccessFlagBits2.Access2TransferWriteBit;
						dstAccessMask = VkAccessFlagBits2.Access2ShaderReadBit;
						srcStageMask = VkPipelineStageFlagBits2.PipelineStage2TransferBit;
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2FragmentShaderBit;
						break;

					default: throw new NotImplementedException();
				}

				return new() {
						oldLayout = oldLayout,
						newLayout = newLayout,
						srcQueueFamilyIndex = transferFamily, // Vk.QueueFamilyIgnored
						dstQueueFamilyIndex = graphicsFamily,
						image = image,
						subresourceRange = new() { aspectMask = aspectMask, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
						srcAccessMask = srcAccessMask,
						dstAccessMask = dstAccessMask,
						srcStageMask = srcStageMask,
						dstStageMask = dstStageMask,
				};
			}
		}

		[Obsolete("Make bulk methods later")] // TODO make bulk methods later
		public void SubmitQueue(VkQueue queue) {
			VkCommandBuffer commandBuffer = VkCommandBuffer;
			SubmitQueue(queue, new() { commandBufferCount = 1, pCommandBuffers = &commandBuffer, });
		}

		[Obsolete("Make bulk methods later")]
		public void SubmitQueue(VkQueue queue, VkSubmitInfo submitInfo, VkFence? fence = null) {
			VkH.CheckIfSuccess(Vk.QueueSubmit(queue, 1, &submitInfo, fence ?? VkFence.Zero), VulkanException.Reason.QueueSubmit); // TODO device lost?
		}
	}
}