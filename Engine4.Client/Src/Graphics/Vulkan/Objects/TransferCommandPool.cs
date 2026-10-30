using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed class TransferCommandPool : CommandPool<TransferCommandBuffer> {
	internal TransferCommandPool(LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(logicalGpu, commandPoolCreateFlags, queueFamily) { }

	[MustUseReturnValue]
	public TransferCommandBuffer[] CreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBuffer[] commandBuffers = InternalCreateCommandBuffers(count, level);

		TransferCommandBuffer[] buffers = new TransferCommandBuffer[count];
		for (int i = 0; i < commandBuffers.Length; i++) { buffers[i] = new(LogicalGpu, VkCommandPool, commandBuffers[i]); }

		CommandBuffers.Add(buffers);
		return buffers;
	}

	[MustUseReturnValue]
	public TransferCommandBuffer CreateCommandBuffer(VkCommandBufferLevel level) {
		TransferCommandBuffer buffer = new(LogicalGpu, VkCommandPool, InternalCreateCommandBuffers(1, level)[0]);
		CommandBuffers.Add(buffer);
		return buffer;
	}
}