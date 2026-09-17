using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class Fence : VulkanResource {
	private readonly VkFence vkFence;

	internal VkFence VkFence => vkFence;
	protected override ulong Handle => VkFence.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Fence(string debugName, LogicalGpu logicalGpu, VkFenceCreateFlagBits fenceCreateFlags) : base(debugName) {
		this.logicalGpu = logicalGpu;

		VkFenceCreateInfo fenceCreateInfo = new() { flags = fenceCreateFlags, };
		VkFence fence;
		VkH.CheckSuccess(Vk.CreateFence(logicalGpu.VkLogicalDevice, &fenceCreateInfo, null, &fence), "Failed to create fence");
		vkFence = fence;
	}

	protected internal override void Cleanup() => Vk.DestroyFence(logicalGpu.VkLogicalDevice, VkFence, null);

	public void Wait(bool waitAll, ulong timeout) {
		fixed (VkFence* fencePtr = &vkFence) { Vk.WaitForFences(logicalGpu.VkLogicalDevice, 1u, fencePtr, waitAll ? VkH.True : VkH.False, timeout); }
	}

	public void Reset() {
		fixed (VkFence* fencePtr = &vkFence) { Vk.ResetFences(logicalGpu.VkLogicalDevice, 1, fencePtr); }
	}
}