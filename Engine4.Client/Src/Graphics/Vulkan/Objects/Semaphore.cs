using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class Semaphore : VulkanResource {
	public VkSemaphore VkSemaphore { get; }
	public override ulong Handle => VkSemaphore.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Semaphore(LogicalGpu logicalGpu, VkSemaphoreCreateFlags semaphoreCreateFlags) {
		VkSemaphore = CreateSemaphore(logicalGpu, semaphoreCreateFlags);
		this.logicalGpu = logicalGpu;
	}

	protected internal override void Cleanup() => Vk.DestroySemaphore(logicalGpu.VkLogicalDevice, VkSemaphore, null);

	[MustUseReturnValue]
	private static VkSemaphore CreateSemaphore(LogicalGpu logicalGpu, VkSemaphoreCreateFlags semaphoreCreateFlags) {
		VkSemaphoreCreateInfo semaphoreCreateInfo = new() { flags = semaphoreCreateFlags, };
		VkSemaphore vkSemaphore;
		VkH.ThrowIfNotSuccess(Vk.CreateSemaphore(logicalGpu.VkLogicalDevice, &semaphoreCreateInfo, null, &vkSemaphore), "Failed to create semaphore");
		return vkSemaphore;
	}
}