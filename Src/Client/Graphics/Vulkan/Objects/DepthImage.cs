using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public class DepthImage {
		public bool WasDestroyed { get; private set; }

		public VulkanImage Image { get; private set; }

		private readonly SurfaceCapablePhysicalGpu physicalGpu;
		private readonly LogicalGpu logicalGpu;
		private readonly VkCommandPool transferCommandPool;
		private readonly VkQueue transferQueue;
		private readonly VkFormat depthFormat;

		internal DepthImage(SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkCommandPool transferCommandPool, VkQueue transferQueue, VkExtent2D extent) {
			this.physicalGpu = physicalGpu;
			this.logicalGpu = logicalGpu;
			this.transferCommandPool = transferCommandPool;
			this.transferQueue = transferQueue;
			depthFormat = physicalGpu.FindDepthFormat();

			Image = logicalGpu.CreateImage("Depth Image", extent.width, extent.height, depthFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit,
				VkImageAspectFlagBits.ImageAspectDepthBit);
		}

		public void Recreate(VkExtent2D extent) {
			logicalGpu.EnqueueDestroy(Image);

			Image = logicalGpu.CreateImage(Image.DebugName, extent.width, extent.height, depthFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit,
				VkImageAspectFlagBits.ImageAspectDepthBit);

			TransferCommandBuffer transferCommandBuffer = logicalGpu.CreateTransferCommandBuffer(transferCommandPool);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image.Image, depthFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			logicalGpu.EnqueueDestroy(transferCommandBuffer);

			WasDestroyed = false;
		}
	}
}