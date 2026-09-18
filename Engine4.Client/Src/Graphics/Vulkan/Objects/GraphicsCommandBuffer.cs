using Engine4.Client.Utility.Extensions;
using Engine4.Utility.Math;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class GraphicsCommandBuffer : CommandBuffer {
	internal GraphicsCommandBuffer(VkCommandBuffer commandBuffer) : base(commandBuffer) { }

	public void CmdBeginRendering(VkExtent2D extent, VkImageView imageView, VkImageView? depthImageView, Color4 color, VkClearDepthStencilValue depthStencilValue) {
		VkRenderingAttachmentInfo colorAttachmentInfo = new() {
				imageView = imageView,
				imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
				loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
				storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
				clearValue = new() { color = color.ToVkClearColorValue(), },
		};

		VkRenderingInfo renderingInfo = new() { renderArea = new() { offset = new(0, 0), extent = extent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &colorAttachmentInfo, };

		if (depthImageView != null) {
			VkRenderingAttachmentInfo depthAttachmentInfo = new() {
					imageView = depthImageView.Value,
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() { depthStencil = depthStencilValue, },
			};

			renderingInfo.pDepthAttachment = &depthAttachmentInfo;
		}

		Vk.CmdBeginRendering(VkCommandBuffer, &renderingInfo);
	}

	public void CmdEndRendering() => Vk.CmdEndRendering(VkCommandBuffer);
}