using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class Semaphore : VulkanResource {
	public override ulong Handle => VkSemaphore.Handle;

	internal VkSemaphore VkSemaphore { get; }

	private readonly LogicalGpu logicalGpu;

	internal Semaphore(string debugName, LogicalGpu logicalGpu, VkSemaphoreCreateFlags semaphoreCreateFlags) : base(debugName) {
		this.logicalGpu = logicalGpu;

		VkSemaphoreCreateInfo semaphoreCreateInfo = new() { flags = semaphoreCreateFlags, };
		VkSemaphore semaphore;
		VkH.ThrowIfNotSuccess(Vk.CreateSemaphore(logicalGpu.VkLogicalDevice, &semaphoreCreateInfo, null, &semaphore), "Failed to create semaphore");
		VkSemaphore = semaphore;
	}

	protected internal override void Cleanup() => Vk.DestroySemaphore(logicalGpu.VkLogicalDevice, VkSemaphore, null);
}