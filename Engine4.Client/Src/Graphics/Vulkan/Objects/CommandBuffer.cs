using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract unsafe class CommandBuffer {
	internal VkCommandBuffer VkCommandBuffer { get; }

	protected CommandBuffer(VkCommandBuffer commandBuffer) => VkCommandBuffer = commandBuffer;

	public void ResetCommandBuffer() => Vk.ResetCommandBuffer(VkCommandBuffer, 0);

	public VkResult BeginCommandBuffer(VkCommandBufferUsageFlagBits bufferUsageFlags) {
		VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = bufferUsageFlags, };
		return Vk.BeginCommandBuffer(VkCommandBuffer, &commandBufferBeginInfo);
	}

	public VkResult EndCommandBuffer() => Vk.EndCommandBuffer(VkCommandBuffer);

	public void CmdPipelineBarrier(VkDependencyInfo dependencyInfo) => Vk.CmdPipelineBarrier2(VkCommandBuffer, &dependencyInfo);
}