using Engine4.Client.Utility.Extensions;
using Engine4.Utility.Math;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class GraphicsCommandBuffer : CommandBuffer {
	internal GraphicsCommandBuffer(VkCommandBuffer commandBuffer) : base(commandBuffer) { }

	public void CmdBeginRendering(VkExtent2D extent, VkImageView colorImageView, Color4 clearColor, VkImageView? depthImageView, VkClearDepthStencilValue? depthStencil) {
		VkRenderingAttachmentInfo colorAttachmentInfo = new() {
				imageView = colorImageView,
				imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
				loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
				storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
				clearValue = new() { color = clearColor.ToVkClearColorValue(), },
		};

		VkRenderingInfo renderingInfo = new() { renderArea = new() { offset = new(0, 0), extent = extent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &colorAttachmentInfo, };

		VkRenderingAttachmentInfo depthAttachmentInfo; // doesn't this need to exist outside the scope?
		if (depthImageView != null && depthStencil != null) {
			depthAttachmentInfo = new() {
					imageView = depthImageView.Value,
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() { depthStencil = depthStencil.Value, },
			};

			renderingInfo.pDepthAttachment = &depthAttachmentInfo;
		}

		Vk.CmdBeginRendering(VkCommandBuffer, &renderingInfo);
	}

	public void CmdEndRendering() => Vk.CmdEndRendering(VkCommandBuffer);
}