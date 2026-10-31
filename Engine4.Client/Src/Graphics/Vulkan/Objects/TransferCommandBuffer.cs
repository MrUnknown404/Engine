using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class TransferCommandBuffer : CommandBuffer {
	internal TransferCommandBuffer(SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkCommandPool vkCommandPool, VkCommandBuffer commandBuffer) : base(physicalGpu, logicalGpu, vkCommandPool, commandBuffer) { }

	// copying
	// TODO copy buffer

	public void CmdCopyImage(VulkanBuffer srcBuffer, VulkanTexture dstImage, uint width, uint height) {
		VkBufferImageCopy2 copyImageInfo2 = new() {
				bufferOffset = 0,
				bufferRowLength = 0,
				bufferImageHeight = 0,
				imageSubresource = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, mipLevel = 0, baseArrayLayer = 0, layerCount = 1, },
				imageOffset = new(0, 0, 0),
				imageExtent = new(width, height, 1),
		};

		VkCopyBufferToImageInfo2 copyBufferToImageInfo2 = new() {
				srcBuffer = srcBuffer.VkBuffer, //
				dstImage = dstImage.Image,
				dstImageLayout = VkImageLayout.ImageLayoutTransferDstOptimal,
				regionCount = 1,
				pRegions = &copyImageInfo2,
		};

		Vk.CmdCopyBufferToImage2(VkCommandBuffer, &copyBufferToImageInfo2);
	}

	public void TransitionImageLayout(VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout) {
		VkImageMemoryBarrier2 imageMemoryBarrier2 = CreateImageBarrier(PhysicalGpu.QueueFamilyIndices, image, format, oldLayout, newLayout);
		CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

		return;

		[SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
		[MustUseReturnValue]
		static VkImageMemoryBarrier2 CreateImageBarrier(QueueFamilyIndices queueFamilyIndices, VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout) {
			// TODO i don't really understand image transitions. read into how image transitions work

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
					srcQueueFamilyIndex = queueFamilyIndices.TransferFamily, // Vk.QueueFamilyIgnored
					dstQueueFamilyIndex = queueFamilyIndices.GraphicsFamily,
					image = image,
					subresourceRange = new() { aspectMask = aspectMask, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
					srcAccessMask = srcAccessMask,
					dstAccessMask = dstAccessMask,
					srcStageMask = srcStageMask,
					dstStageMask = dstStageMask,
			};
		}
	}
}