using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract unsafe class CommandBuffer : VulkanResource {
	public override ulong Handle => (ulong)VkCommandBuffer.Handle;

	private readonly VkCommandBuffer vkCommandBuffer;
	internal VkCommandBuffer VkCommandBuffer => vkCommandBuffer;

	private readonly LogicalGpu logicalGpu;
	private readonly VkCommandPool vkCommandPool;

	protected CommandBuffer(string debugName, LogicalGpu logicalGpu, VkCommandPool vkCommandPool, VkCommandBuffer commandBuffer) : base(debugName) {
		this.logicalGpu = logicalGpu;
		this.vkCommandPool = vkCommandPool;
		vkCommandBuffer = commandBuffer;
	}

	public void ResetCommandBuffer() => Vk.ResetCommandBuffer(VkCommandBuffer, 0);

	public VkResult BeginCommandBuffer(VkCommandBufferUsageFlagBits bufferUsageFlags) {
		VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = bufferUsageFlags, };
		return Vk.BeginCommandBuffer(VkCommandBuffer, &commandBufferBeginInfo);
	}

	public VkResult EndCommandBuffer() => Vk.EndCommandBuffer(VkCommandBuffer);

	public void CmdPipelineBarrier(VkDependencyInfo dependencyInfo) => Vk.CmdPipelineBarrier2(VkCommandBuffer, &dependencyInfo);

	protected internal override void Cleanup() {
		fixed (VkCommandBuffer* pVkCommandBuffer = &vkCommandBuffer) { Vk.FreeCommandBuffers(logicalGpu.VkLogicalDevice, vkCommandPool, 1, pVkCommandBuffer); }
	}
}