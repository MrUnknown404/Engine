using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public class DepthImage {
		public VulkanImage Image { get; private set; }

		private readonly SurfaceCapablePhysicalGpu physicalGpu;
		private readonly VulkanResourceProvider graphicsResourceProvider;
		private readonly TransferCommandPool transferCommandPool;
		private readonly VkQueue transferQueue;
		private readonly VkFormat depthFormat;

		internal DepthImage(SurfaceCapablePhysicalGpu physicalGpu, VulkanResourceProvider graphicsResourceProvider, TransferCommandPool transferCommandPool, VkQueue transferQueue, VkExtent2D extent) {
			this.physicalGpu = physicalGpu;
			this.graphicsResourceProvider = graphicsResourceProvider;
			this.transferCommandPool = transferCommandPool;
			this.transferQueue = transferQueue;
			depthFormat = physicalGpu.FindDepthFormat();

			Image = graphicsResourceProvider.CreateImage("Depth Image", extent.width, extent.height, depthFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit,
				VkImageAspectFlagBits.ImageAspectDepthBit);
		}

		public void Recreate(VkExtent2D extent) {
			graphicsResourceProvider.EnqueueDestroy(Image);

			Image = graphicsResourceProvider.CreateImage(Image.DebugName, extent.width, extent.height, depthFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit,
				VkImageAspectFlagBits.ImageAspectDepthBit);

			TransferCommandBuffer transferCommandBuffer = transferCommandPool.CreateCommandBuffer();
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image.Image, depthFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			graphicsResourceProvider.EnqueueDestroy(transferCommandBuffer);
		}
	}
}