using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract unsafe class CommandPool<T> : VulkanResource where T : CommandBuffer {
	public VkCommandPool VkCommandPool { get; }
	public override ulong Handle => VkCommandPool.Handle;

	protected ResourceList<T> CommandBuffers { get; } = new();
	protected LogicalGpu LogicalGpu { get; }

	protected CommandPool(LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) {
		VkCommandPool = CreateCommandPool(logicalGpu, commandPoolCreateFlags, queueFamily);
		LogicalGpu = logicalGpu;
	}

	internal VkCommandBuffer[] InternalCreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = VkCommandPool, level = level, commandBufferCount = count, };
		VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];

		fixed (VkCommandBuffer* pCommandBuffers = commandBuffers) {
			VkH.ThrowIfNotSuccess(Vk.AllocateCommandBuffers(LogicalGpu.VkLogicalDevice, &commandBufferAllocateInfo, pCommandBuffers), "Failed to allocate command buffers");
			return commandBuffers;
		}
	}

	protected internal override void Cleanup() {
		CommandBuffers.Cleanup();
		Vk.DestroyCommandPool(LogicalGpu.VkLogicalDevice, VkCommandPool, null);
	}

	[MustUseReturnValue]
	private static VkCommandPool CreateCommandPool(LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) {
		VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamily, };
		VkCommandPool commandPool;
		VkH.ThrowIfNotSuccess(Vk.CreateCommandPool(logicalGpu.VkLogicalDevice, &commandPoolCreateInfo, null, &commandPool), "Failed to create command pool");
		return commandPool;
	}
}