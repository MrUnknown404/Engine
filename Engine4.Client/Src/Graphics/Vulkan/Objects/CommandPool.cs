using Engine4.Client.Graphics.Vulkan.Resources;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract unsafe class CommandPool : VulkanResource {
	protected override ulong Handle => VkCommandPool.Handle;

	protected List<CommandBuffer> CommandBuffers { get; } = new();

	private readonly LogicalGpu logicalGpu;
	protected VkCommandPool VkCommandPool { get; }

	protected CommandPool(string debugName, LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(debugName) {
		this.logicalGpu = logicalGpu;

		VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamily, };
		VkCommandPool commandPool;
		if (Vk.CreateCommandPool(logicalGpu.VkLogicalDevice, &commandPoolCreateInfo, null, &commandPool) != VkResult.Success) { throw new Exception(); } // TODO exception
		VkCommandPool = commandPool;
	}

	protected internal override void Cleanup() {
		VkCommandBuffer[] commandBuffers = CommandBuffers.Select(static b => b.VkCommandBuffer).ToArray();
		fixed (VkCommandBuffer* commandBufferPtr = commandBuffers) { Vk.FreeCommandBuffers(logicalGpu.VkLogicalDevice, VkCommandPool, (uint)commandBuffers.Length, commandBufferPtr); } // cleanup buffers
	}

	protected VkCommandBuffer[] InternalCreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = VkCommandPool, level = level, commandBufferCount = count, };
		VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];

		fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
			return Vk.AllocateCommandBuffers(logicalGpu.VkLogicalDevice, &commandBufferAllocateInfo, commandBuffersPtr) == VkResult.Success ? commandBuffers : throw new Exception(); // TODO exception
		}
	}
}