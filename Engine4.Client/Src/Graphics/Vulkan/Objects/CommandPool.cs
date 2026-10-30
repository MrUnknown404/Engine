using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract unsafe class CommandPool<T> : VulkanResource where T : CommandBuffer {
	protected ResourceList<T> CommandBuffers { get; } = new();

	public VkCommandPool VkCommandPool { get; }
	public override ulong Handle => VkCommandPool.Handle;

	protected LogicalGpu LogicalGpu { get; }

	protected CommandPool(LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) {
		LogicalGpu = logicalGpu;

		VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamily, };
		VkCommandPool commandPool;
		VkH.ThrowIfNotSuccess(Vk.CreateCommandPool(logicalGpu.VkLogicalDevice, &commandPoolCreateInfo, null, &commandPool), "Failed to create command pool");
		VkCommandPool = commandPool;
	}

	internal VkCommandBuffer[] InternalCreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = VkCommandPool, level = level, commandBufferCount = count, };
		VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];

		fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
			VkH.ThrowIfNotSuccess(Vk.AllocateCommandBuffers(LogicalGpu.VkLogicalDevice, &commandBufferAllocateInfo, commandBuffersPtr), "Failed to allocate command buffers");
			return commandBuffers;
		}
	}

	protected internal override void Cleanup() {
		CommandBuffers.Cleanup();
		Vk.DestroyCommandPool(LogicalGpu.VkLogicalDevice, VkCommandPool, null);
	}
}