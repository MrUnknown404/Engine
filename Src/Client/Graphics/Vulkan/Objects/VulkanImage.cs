using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class VulkanImage : IGraphicsResource {
		public VkImage Image { get; }
		public VkDeviceMemory ImageMemory { get; }
		public VkImageView ImageView { get; }
		public VkFormat ImageFormat { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly PhysicalGpu physicalGpu;
		private readonly LogicalGpu logicalGpu;

		internal VulkanImage(string debugName, PhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkImage image, VkDeviceMemory imageMemory, VkImageView imageView, VkFormat imageFormat) {
			DebugName = debugName;
			this.physicalGpu = physicalGpu;
			this.logicalGpu = logicalGpu;
			Image = image;
			ImageMemory = imageMemory;
			ImageView = imageView;
			ImageFormat = imageFormat;
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			VkDevice logicalDevice = logicalGpu.LogicalDevice;

			Vk.DestroyImageView(logicalDevice, ImageView, null);
			Vk.DestroyImage(logicalDevice, Image, null);
			Vk.FreeMemory(logicalDevice, ImageMemory, null);

			WasDestroyed = true;
		}

		public void Copy(VkCommandPool transferCommandPool, VkQueue transferQueue, StbiImage stbiImage, byte texChannels) {
			uint width = (uint)stbiImage.Width;
			uint height = (uint)stbiImage.Height;

			VulkanBuffer stagingBuffer = logicalGpu.CreateBuffer("Temporary Image Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, width * height * texChannels);

			stagingBuffer.Copy(stbiImage.Data);

			TransferCommandBuffer transferCommandBuffer = logicalGpu.CreateTransferCommandBuffer(transferCommandPool);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image, ImageFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutTransferDstOptimal);
			transferCommandBuffer.CmdCopyImage(stagingBuffer.Buffer, Image, width, height);
			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image, ImageFormat, VkImageLayout.ImageLayoutTransferDstOptimal, VkImageLayout.ImageLayoutShaderReadOnlyOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			Vk.QueueWaitIdle(transferQueue);
			transferCommandBuffer.Destroy();

			stagingBuffer.Destroy();
		}
	}
}