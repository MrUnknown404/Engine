using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class TransferCommandPool : CommandPool<TransferCommandBuffer> {
	internal TransferCommandPool(string debugName, LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(debugName, logicalGpu, commandPoolCreateFlags, queueFamily) { }

	[MustUseReturnValue]
	public TransferCommandBuffer[] CreateCommandBuffers(string debugName, byte count, VkCommandBufferLevel level) {
		VkCommandBuffer[] commandBuffers = InternalCreateCommandBuffers(count, level);

		TransferCommandBuffer[] buffers = new TransferCommandBuffer[count];
		for (int i = 0; i < commandBuffers.Length; i++) { buffers[i] = new($"{debugName} [{i}]", LogicalGpu, VkCommandPool, commandBuffers[i]); }

		CommandBuffers.Add(buffers);
		return buffers;
	}

	[MustUseReturnValue]
	public TransferCommandBuffer CreateCommandBuffer(string debugName, VkCommandBufferLevel level) {
		TransferCommandBuffer buffer = new(debugName, LogicalGpu, VkCommandPool, InternalCreateCommandBuffers(1, level)[0]);
		CommandBuffers.Add(buffer);
		return buffer;
	}
}