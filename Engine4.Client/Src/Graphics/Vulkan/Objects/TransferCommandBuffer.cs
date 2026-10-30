using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed class TransferCommandBuffer : CommandBuffer {
	internal TransferCommandBuffer(LogicalGpu logicalGpu, VkCommandPool vkCommandPool, VkCommandBuffer commandBuffer) : base(logicalGpu, vkCommandPool, commandBuffer) { }
}