using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class Semaphore : VulkanResource {
	private readonly VkSemaphore vkSemaphore;
	public VkSemaphore VkSemaphore => vkSemaphore;
	public override ulong Handle => VkSemaphore.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Semaphore(LogicalGpu logicalGpu, VkSemaphoreCreateFlags semaphoreCreateFlags) {
		this.logicalGpu = logicalGpu;

		VkSemaphoreCreateInfo semaphoreCreateInfo = new() { flags = semaphoreCreateFlags, };
		fixed (VkSemaphore* pVkSemaphore = &vkSemaphore) { VkH.ThrowIfNotSuccess(Vk.CreateSemaphore(logicalGpu.VkLogicalDevice, &semaphoreCreateInfo, null, pVkSemaphore), "Failed to create semaphore"); }
	}

	protected internal override void Cleanup() => Vk.DestroySemaphore(logicalGpu.VkLogicalDevice, VkSemaphore, null);
}