using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class GraphicsCommandPool : CommandPool {
	internal GraphicsCommandPool(string debugName, LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(debugName, logicalGpu, commandPoolCreateFlags, queueFamily) { }

	[MustUseReturnValue]
	public GraphicsCommandBuffer[] CreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBuffer[] commandBuffers = InternalCreateCommandBuffers(count, level);

		GraphicsCommandBuffer[] buffers = new GraphicsCommandBuffer[count];
		for (int i = 0; i < commandBuffers.Length; i++) { buffers[i] = new(commandBuffers[i]); }

		CommandBuffers.AddRange(buffers);
		return buffers;
	}

	[MustUseReturnValue]
	public GraphicsCommandBuffer CreateCommandBuffer(VkCommandBufferLevel level) => CreateCommandBuffers(1, level)[0];
}