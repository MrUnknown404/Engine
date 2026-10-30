using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class Fence : VulkanResource {
	private readonly VkFence vkFence;
	public VkFence VkFence => vkFence;
	public override ulong Handle => VkFence.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Fence(LogicalGpu logicalGpu, VkFenceCreateFlagBits fenceCreateFlags) {
		this.logicalGpu = logicalGpu;

		VkFenceCreateInfo fenceCreateInfo = new() { flags = fenceCreateFlags, };
		fixed (VkFence* pVkFence = &vkFence) { VkH.ThrowIfNotSuccess(Vk.CreateFence(logicalGpu.VkLogicalDevice, &fenceCreateInfo, null, pVkFence), "Failed to create fence"); }
	}

	public void Wait(bool waitAll, ulong timeout) {
		fixed (VkFence* pVkFence = &vkFence) { Vk.WaitForFences(logicalGpu.VkLogicalDevice, 1u, pVkFence, waitAll ? VkH.True : VkH.False, timeout); }
	}

	public void Reset() {
		fixed (VkFence* pVkFence = &vkFence) { Vk.ResetFences(logicalGpu.VkLogicalDevice, 1, pVkFence); }
	}

	protected internal override void Cleanup() => Vk.DestroyFence(logicalGpu.VkLogicalDevice, VkFence, null);
}