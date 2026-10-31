using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class Queue : VulkanResource {
	public VkQueue VkQueue { get; }
	public override ulong Handle => (ulong)VkQueue.Handle;

	internal Queue(LogicalGpu logicalGpu, uint graphicsFamily) => VkQueue = GetDeviceQueue(logicalGpu.VkLogicalDevice, graphicsFamily);

	public void Submit(VkSubmitInfo submitInfo, Fence? fence) => Vk.QueueSubmit(VkQueue, 1, &submitInfo, fence?.VkFence ?? VkFence.Zero);

	public void Submit(VkSubmitInfo[] submitInfos, Fence? fence) {
		fixed (VkSubmitInfo* pSubmitInfos = submitInfos) { Vk.QueueSubmit(VkQueue, (uint)submitInfos.Length, pSubmitInfos, fence?.VkFence ?? VkFence.Zero); }
	}

	public VkResult Present(VkPresentInfoKHR presentInfo) => Vk.QueuePresentKHR(VkQueue, &presentInfo);

	protected internal override void Cleanup() { } // no cleanup needed

	[MustUseReturnValue]
	private static VkQueue GetDeviceQueue(VkDevice logicalDevice, uint queueFamilyIndex) {
		VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
		VkQueue queue;
		Vk.GetDeviceQueue2(logicalDevice, &deviceQueueInfo2, &queue);
		return queue;
	}
}