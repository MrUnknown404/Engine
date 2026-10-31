using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract unsafe class CommandBuffer : VulkanResource {
	private readonly VkCommandBuffer vkCommandBuffer;
	public VkCommandBuffer VkCommandBuffer => vkCommandBuffer;
	public override ulong Handle => (ulong)VkCommandBuffer.Handle;

	protected SurfaceReadyPhysicalGpu PhysicalGpu { get; }
	protected LogicalGpu LogicalGpu { get; }

	private readonly VkCommandPool vkCommandPool;

	protected CommandBuffer(SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VkCommandPool vkCommandPool, VkCommandBuffer commandBuffer) {
		PhysicalGpu = physicalGpu;
		LogicalGpu = logicalGpu;
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
		fixed (VkCommandBuffer* pVkCommandBuffer = &vkCommandBuffer) { Vk.FreeCommandBuffers(LogicalGpu.VkLogicalDevice, vkCommandPool, 1, pVkCommandBuffer); }
	}
}