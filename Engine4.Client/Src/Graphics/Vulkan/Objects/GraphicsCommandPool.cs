using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed class GraphicsCommandPool : CommandPool<GraphicsCommandBuffer> {
	internal GraphicsCommandPool(LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(logicalGpu, commandPoolCreateFlags, queueFamily) { }

	[MustUseReturnValue]
	public GraphicsCommandBuffer[] CreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBuffer[] commandBuffers = InternalCreateCommandBuffers(count, level);

		GraphicsCommandBuffer[] buffers = new GraphicsCommandBuffer[count];
		for (int i = 0; i < commandBuffers.Length; i++) { buffers[i] = new(LogicalGpu, VkCommandPool, commandBuffers[i]); }

		CommandBuffers.Add(buffers);
		return buffers;
	}

	[MustUseReturnValue]
	public GraphicsCommandBuffer CreateCommandBuffer(VkCommandBufferLevel level) {
		GraphicsCommandBuffer buffer = new(LogicalGpu, VkCommandPool, InternalCreateCommandBuffers(1, level)[0]);
		CommandBuffers.Add(buffer);
		return buffer;
	}
}