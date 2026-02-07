using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class VulkanImage : NamedGraphicsResource<VulkanImage, ulong> {
		public VkImage Image { get; }
		public VkDeviceMemory ImageMemory { get; }
		public VkImageView ImageView { get; }
		public VkFormat ImageFormat { get; }

		protected override ulong Handle => Image.Handle;

		private readonly SurfaceCapablePhysicalGpu physicalGpu;
		private readonly LogicalGpu logicalGpu;

		internal VulkanImage(string debugName, SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkImage image, VkDeviceMemory imageMemory, VkImageView imageView, VkFormat imageFormat) : base(debugName) {
			this.physicalGpu = physicalGpu;
			this.logicalGpu = logicalGpu;
			Image = image;
			ImageMemory = imageMemory;
			ImageView = imageView;
			ImageFormat = imageFormat;

			PrintCreate();
		}

		[Obsolete] // TODO move elsewhere
		public void CopyUsingStaging(VkCommandPool transferCommandPool, VkQueue transferQueue, StbiImage stbiImage) =>
				CopyUsingStaging(transferCommandPool, transferQueue, (uint)stbiImage.Width, (uint)stbiImage.Height, (uint)stbiImage.NumChannels, stbiImage.Data);

		[Obsolete] // TODO move elsewhere
		public void CopyUsingStaging(VkCommandPool transferCommandPool, VkQueue transferQueue, uint width, uint height, uint channels, ReadOnlySpan<byte> data) {
			VulkanBuffer stagingBuffer = logicalGpu.CreateBuffer("Temporary Image Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, width * height * channels);

			stagingBuffer.Copy(data);

			TransferCommandBuffer transferCommandBuffer = logicalGpu.CreateTransferCommandBuffer(transferCommandPool);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image, ImageFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutTransferDstOptimal);
			transferCommandBuffer.CmdCopyImage(stagingBuffer.Buffer, Image, width, height);
			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image, ImageFormat, VkImageLayout.ImageLayoutTransferDstOptimal, VkImageLayout.ImageLayoutShaderReadOnlyOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			logicalGpu.EnqueueDestroy(transferCommandBuffer);
			logicalGpu.EnqueueDestroy(stagingBuffer);
		}

		[Obsolete]
		public void CopyUsingStaging(VkCommandPool transferCommandPool, VkQueue transferQueue, uint width, uint height, uint channels, byte* data) { // TODO move elsewhere
			VulkanBuffer stagingBuffer = logicalGpu.CreateBuffer("Temporary Image Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, width * height * channels);

			stagingBuffer.Copy(data, width * height * channels);

			TransferCommandBuffer transferCommandBuffer = logicalGpu.CreateTransferCommandBuffer(transferCommandPool);
			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image, ImageFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutTransferDstOptimal);
			transferCommandBuffer.CmdCopyImage(stagingBuffer.Buffer, Image, width, height);
			transferCommandBuffer.TransitionImageLayout(physicalGpu.QueueFamilyIndices, Image, ImageFormat, VkImageLayout.ImageLayoutTransferDstOptimal, VkImageLayout.ImageLayoutShaderReadOnlyOptimal);

			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			logicalGpu.EnqueueDestroy(transferCommandBuffer);
			logicalGpu.EnqueueDestroy(stagingBuffer);
		}

		protected override void Cleanup() {
			VkDevice logicalDevice = logicalGpu.LogicalDevice;

			Vk.DestroyImageView(logicalDevice, ImageView, null);
			Vk.DestroyImage(logicalDevice, Image, null);
			Vk.FreeMemory(logicalDevice, ImageMemory, null);
		}
	}
}