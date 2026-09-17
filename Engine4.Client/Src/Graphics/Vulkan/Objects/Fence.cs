using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class Fence : VulkanResource {
	internal VkFence VkFence { get; } //  TODO private
	protected override ulong Handle => VkFence.Handle;

	private readonly LogicalGpu logicalGpu;

	internal Fence(string debugName, LogicalGpu logicalGpu, VkFenceCreateFlagBits fenceCreateFlags) : base(debugName) {
		this.logicalGpu = logicalGpu;
		VkFenceCreateInfo fenceCreateInfo = new() { flags = fenceCreateFlags, };
		VkFence fence;
		VkFence = Vk.CreateFence(logicalGpu.VkLogicalDevice, &fenceCreateInfo, null, &fence) == VkResult.Success ? fence : throw new Exception(); // TODO exception
	}

	protected internal override void Cleanup() => Vk.DestroyFence(logicalGpu.VkLogicalDevice, VkFence, null);
}