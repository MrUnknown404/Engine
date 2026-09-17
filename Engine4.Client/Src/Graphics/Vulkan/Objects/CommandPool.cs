using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract unsafe class CommandPool : VulkanResource {
	protected override ulong Handle => vkCommandPool.Handle;

	protected List<CommandBuffer> CommandBuffers { get; } = new();

	private readonly LogicalGpu logicalGpu;
	private readonly VkCommandPool vkCommandPool;

	protected CommandPool(string debugName, LogicalGpu logicalGpu, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamily) : base(debugName) {
		this.logicalGpu = logicalGpu;

		VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamily, };
		VkCommandPool commandPool;
		VkH.CheckSuccess(Vk.CreateCommandPool(logicalGpu.VkLogicalDevice, &commandPoolCreateInfo, null, &commandPool), "Failed to create command pool");
		vkCommandPool = commandPool;
	}

	internal VkCommandBuffer[] InternalCreateCommandBuffers(byte count, VkCommandBufferLevel level) {
		VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = vkCommandPool, level = level, commandBufferCount = count, };
		VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];

		fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
			VkH.CheckSuccess(Vk.AllocateCommandBuffers(logicalGpu.VkLogicalDevice, &commandBufferAllocateInfo, commandBuffersPtr), "Failed to allocate command buffers");
			return commandBuffers;
		}
	}

	protected internal override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;

		if (CommandBuffers.Count != 0) { // cleanup buffers
			VkCommandBuffer[] commandBuffers = CommandBuffers.Select(static b => b.VkCommandBuffer).ToArray();
			fixed (VkCommandBuffer* commandBufferPtr = commandBuffers) { Vk.FreeCommandBuffers(logicalDevice, vkCommandPool, (uint)commandBuffers.Length, commandBufferPtr); }
		}

		Vk.DestroyCommandPool(logicalDevice, vkCommandPool, null);
	}
}