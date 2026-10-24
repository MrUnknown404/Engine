using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class TransferCommandBuffer : CommandBuffer {
	internal TransferCommandBuffer(string debugName, LogicalGpu logicalGpu, VkCommandPool vkCommandPool, VkCommandBuffer commandBuffer) : base(debugName, logicalGpu, vkCommandPool, commandBuffer) { }
}