using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class GraphicsCommandPool : CommandPool<GraphicsCommandBuffer> {
	internal GraphicsCommandPool(string debugName, LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(debugName, logicalGpu, commandPoolCreateFlags, queueFamily) { }

	[MustUseReturnValue]
	public GraphicsCommandBuffer[] CreateCommandBuffers(string debugName, byte count, VkCommandBufferLevel level) {
		VkCommandBuffer[] commandBuffers = InternalCreateCommandBuffers(count, level);

		GraphicsCommandBuffer[] buffers = new GraphicsCommandBuffer[count];
		for (int i = 0; i < commandBuffers.Length; i++) { buffers[i] = new($"{debugName} [{i}]", LogicalGpu, VkCommandPool, commandBuffers[i]); }

		CommandBuffers.Add(buffers);
		return buffers;
	}

	[MustUseReturnValue]
	public GraphicsCommandBuffer CreateCommandBuffer(string debugName, VkCommandBufferLevel level) {
		GraphicsCommandBuffer buffer = new(debugName, LogicalGpu, VkCommandPool, InternalCreateCommandBuffers(1, level)[0]);
		CommandBuffers.Add(buffer);
		return buffer;
	}
}