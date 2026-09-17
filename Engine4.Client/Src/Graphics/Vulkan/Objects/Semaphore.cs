using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class Semaphore : VulkanResource {
	internal VkSemaphore VkSemaphore { get; } //  TODO private
	protected override ulong Handle => VkSemaphore.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Semaphore(string debugName, LogicalGpu logicalGpu, VkSemaphoreCreateFlags semaphoreCreateFlags) : base(debugName) {
		this.logicalGpu = logicalGpu;

		VkSemaphoreCreateInfo semaphoreCreateInfo = new() { flags = semaphoreCreateFlags, };
		VkSemaphore semaphore;
		VkSemaphore = Vk.CreateSemaphore(logicalGpu.VkLogicalDevice, &semaphoreCreateInfo, null, &semaphore) == VkResult.Success ? semaphore : throw new Exception(); // TODO exception
	}

	protected internal override void Cleanup() => Vk.DestroySemaphore(logicalGpu.VkLogicalDevice, VkSemaphore, null);
}