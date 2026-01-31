using Engine3.Utility;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public class DepthImage : IGraphicsResource {
		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public VulkanImage Image { get; private set; }

		private readonly PhysicalGpu physicalGpu;
		private readonly LogicalGpu logicalGpu;
		private readonly VkCommandPool transferCommandPool;
		private readonly VkQueue transferQueue;
		private readonly VkFormat depthFormat;

		internal DepthImage(string debugName, PhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkCommandPool transferCommandPool, VkQueue transferQueue, VkExtent2D extent) {
			DebugName = debugName;
			this.physicalGpu = physicalGpu;
			this.logicalGpu = logicalGpu;
			this.transferCommandPool = transferCommandPool;
			this.transferQueue = transferQueue;
			depthFormat = physicalGpu.FindDepthFormat();

			Image = logicalGpu.CreateImage("Depth Image", extent.width, extent.height, depthFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit,
				VkImageAspectFlagBits.ImageAspectDepthBit);
		}

		public void Recreate(VkExtent2D extent) {
			Image.Destroy();

			Image = logicalGpu.CreateImage(Image.DebugName, extent.width, extent.height, depthFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageDepthStencilAttachmentBit,
				VkImageAspectFlagBits.ImageAspectDepthBit);

			TransferCommandBuffer transferCommandBuffer = logicalGpu.CreateTransferCommandBuffer(transferCommandPool);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image.Image, depthFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			Vk.QueueWaitIdle(transferQueue);
			transferCommandBuffer.Destroy();

			WasDestroyed = false;
		}

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Image.Destroy();

			WasDestroyed = true;
		}
	}
}