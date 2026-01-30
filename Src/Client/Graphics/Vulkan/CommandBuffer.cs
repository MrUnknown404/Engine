using Engine3.Exceptions;
using Engine3.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public abstract unsafe class CommandBuffer : IDestroyable {
		public VkCommandPool CommandPool { get; }
		public VkCommandBuffer VkCommandBuffer { get; }
		public VkQueue Queue { get; }

		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		protected CommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBuffer commandBuffer, VkQueue queue) {
			this.logicalDevice = logicalDevice;
			CommandPool = commandPool;
			VkCommandBuffer = commandBuffer;
			Queue = queue;
		}

		public void ResetCommandBuffer() => Vk.ResetCommandBuffer(VkCommandBuffer, 0);

		public VkResult BeginCommandBuffer(VkCommandBufferUsageFlagBits bufferUsageFlags = 0) {
			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = bufferUsageFlags, };
			return Vk.BeginCommandBuffer(VkCommandBuffer, &commandBufferBeginInfo);
		}

		public VkResult EndCommandBuffer() => Vk.EndCommandBuffer(VkCommandBuffer);

		public void CmdPipelineBarrier(VkDependencyInfo dependencyInfo) => Vk.CmdPipelineBarrier2(VkCommandBuffer, &dependencyInfo);

		public void SubmitQueue() {
			VkCommandBuffer commandBuffer = VkCommandBuffer;
			SubmitQueue(new() { commandBufferCount = 1, pCommandBuffers = &commandBuffer, });
		}

		public void SubmitQueue(VkSubmitInfo submitInfo, VkFence? fence = null) {
			VkH.CheckIfSuccess(Vk.QueueSubmit(Queue, 1, &submitInfo, fence ?? VkFence.Zero), VulkanException.Reason.QueueSubmit); // TODO device lost?
		}

		[MustUseReturnValue]
		protected static VkCommandBuffer CreateCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = 1, };
			VkCommandBuffer commandBuffers;
			VkH.CheckIfSuccess(Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, &commandBuffers), VulkanException.Reason.AllocateCommandBuffer);
			return commandBuffers;
		}

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Vk.QueueWaitIdle(Queue);
			VkCommandBuffer commandBuffers = VkCommandBuffer;
			Vk.FreeCommandBuffers(logicalDevice, CommandPool, 1, &commandBuffers);

			WasDestroyed = true;
		}
	}
}