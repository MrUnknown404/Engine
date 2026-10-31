using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class Fence : VulkanResource {
	private readonly VkFence vkFence;
	public VkFence VkFence => vkFence;
	public override ulong Handle => VkFence.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Fence(LogicalGpu logicalGpu, VkFenceCreateFlagBits fenceCreateFlags) {
		vkFence = CreateFence(logicalGpu, fenceCreateFlags);
		this.logicalGpu = logicalGpu;
	}

	public void Wait(bool waitAll, ulong timeout) {
		fixed (VkFence* pVkFence = &vkFence) { Vk.WaitForFences(logicalGpu.VkLogicalDevice, 1u, pVkFence, waitAll ? VkH.True : VkH.False, timeout); }
	}

	public void Reset() {
		fixed (VkFence* pVkFence = &vkFence) { Vk.ResetFences(logicalGpu.VkLogicalDevice, 1, pVkFence); }
	}

	protected internal override void Cleanup() => Vk.DestroyFence(logicalGpu.VkLogicalDevice, VkFence, null);

	[MustUseReturnValue]
	private static VkFence CreateFence(LogicalGpu logicalGpu, VkFenceCreateFlagBits fenceCreateFlags) {
		VkFenceCreateInfo fenceCreateInfo = new() { flags = fenceCreateFlags, };
		VkFence vkFence;
		VkH.ThrowIfNotSuccess(Vk.CreateFence(logicalGpu.VkLogicalDevice, &fenceCreateInfo, null, &vkFence), "Failed to create fence");
		return vkFence;
	}
}