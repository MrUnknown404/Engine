using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public class TransferCommandBuffer : CommandBuffer {
	internal TransferCommandBuffer(VkCommandBuffer commandBuffer) : base(commandBuffer) { }
}