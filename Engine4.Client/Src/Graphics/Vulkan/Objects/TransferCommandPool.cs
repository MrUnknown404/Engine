using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class TransferCommandPool : CommandPool<TransferCommandBuffer> {
	private readonly VulkanResourceManager resourceManager;

	internal TransferCommandPool(VulkanResourceManager resourceManager, SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(
		physicalGpu, logicalGpu, commandPoolCreateFlags, queueFamily) =>
			this.resourceManager = resourceManager;

	[MustUseReturnValue]
	public TransferCommandBuffer[] CreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBuffer[] commandBuffers = InternalCreateCommandBuffers(count, level);

		TransferCommandBuffer[] buffers = new TransferCommandBuffer[count];
		for (int i = 0; i < commandBuffers.Length; i++) { buffers[i] = new(PhysicalGpu, LogicalGpu, VkCommandPool, commandBuffers[i]); }

		CommandBuffers.Add(buffers);
		return buffers;
	}

	[MustUseReturnValue]
	public TransferCommandBuffer CreateCommandBuffer(VkCommandBufferLevel level) {
		TransferCommandBuffer buffer = new(PhysicalGpu, LogicalGpu, VkCommandPool, InternalCreateCommandBuffers(1, level)[0]);
		CommandBuffers.Add(buffer);
		return buffer;
	}

	// TODO load image from file
	// public void CopyToImage(Texture image, QueueFamilyIndices queueFamilyIndices, Queue transferQueue, StbiImage stbiImage) =>
	// 		CopyToImage(image, queueFamilyIndices, transferQueue, (uint)stbiImage.Width, (uint)stbiImage.Height, (byte)stbiImage.NumChannels, stbiImage.Data);

	public void CopyToImage(VulkanTexture image, uint width, uint height, byte channels, ReadOnlySpan<byte> data) {
		fixed (byte* pData = data) { CopyToImage(image, width, height, channels, pData); }
	}

	public void CopyToImage(VulkanTexture image, uint width, uint height, byte channels, void* data) {
		// TODO persistent staging buffer? (also transferCommandBuffer?)
		VulkanBuffer stagingBuffer = resourceManager.CreateBuffer("Temporary Image Staging Buffer", width * height * channels, VkBufferUsageFlagBits2.BufferUsage2TransferSrcBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

		stagingBuffer.Copy(data, width * height * channels);

		TransferCommandBuffer transferCommandBuffer = CreateCommandBuffer(VkCommandBufferLevel.CommandBufferLevelPrimary);
		transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

		transferCommandBuffer.TransitionImageLayout(image.Image, image.ImageFormat, VkImageLayout.ImageLayoutUndefined, VkImageLayout.ImageLayoutTransferDstOptimal);
		transferCommandBuffer.CmdCopyImage(stagingBuffer, image, width, height);
		transferCommandBuffer.TransitionImageLayout(image.Image, image.ImageFormat, VkImageLayout.ImageLayoutTransferDstOptimal, VkImageLayout.ImageLayoutShaderReadOnlyOptimal);

		transferCommandBuffer.EndCommandBuffer();

		VkCommandBuffer vkCommandBuffer = transferCommandBuffer.VkCommandBuffer; // copy
		LogicalGpu.TransferQueue.Submit(new VkSubmitInfo { commandBufferCount = 1, pCommandBuffers = &vkCommandBuffer, }, null);

		LogicalGpu.WaitIdle();
		CommandBuffers.RemoveNow(transferCommandBuffer);
		resourceManager.EnqueueDestroy(stagingBuffer);
	}
}