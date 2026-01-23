using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public abstract unsafe class CommandBuffer {
		public VkCommandPool CommandPool { get; }
		public VkCommandBuffer VkCommandBuffer { get; }

		private readonly VkDevice logicalDevice;

		protected CommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBuffer vkCommandBuffer) {
			this.logicalDevice = logicalDevice;
			CommandPool = commandPool;
			VkCommandBuffer = vkCommandBuffer;
		}

		public void ResetCommandBuffer() => Vk.ResetCommandBuffer(VkCommandBuffer, 0);

		public VkResult BeginCommandBuffer(VkCommandBufferUsageFlagBits bufferUsageFlags = 0) {
			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = bufferUsageFlags, };
			return Vk.BeginCommandBuffer(VkCommandBuffer, &commandBufferBeginInfo);
		}

		public VkResult EndCommandBuffer() => Vk.EndCommandBuffer(VkCommandBuffer);

		public void FreeCommandBuffers() {
			VkCommandBuffer commandBuffers = VkCommandBuffer;
			Vk.FreeCommandBuffers(logicalDevice, CommandPool, 1, &commandBuffers);
		}

		protected static void SubmitQueues(VkQueue queue, VkSubmitInfo[] submitInfos, VkFence? fence) {
			fixed (VkSubmitInfo* submitInfosPtr = submitInfos) {
				VkResult result = Vk.QueueSubmit(queue, (uint)submitInfos.Length, submitInfosPtr, fence ?? VkFence.Zero);
				if (result != VkResult.Success) { throw new VulkanException($"Failed to submit queue. {result}"); }
			}
		}

		protected static void SubmitQueue(VkQueue queue, VkSubmitInfo submitInfo, VkFence? fence) {
			VkResult result = Vk.QueueSubmit(queue, 1, &submitInfo, fence ?? VkFence.Zero);
			if (result != VkResult.Success) { throw new VulkanException($"Failed to submit queue. {result}"); } // TODO device lost?
		}

		[MustUseReturnValue]
		protected static VkCommandBuffer CreateCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = 1, };
			VkCommandBuffer commandBuffers;
			VkResult result = Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, &commandBuffers);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create command buffer. {result}") : commandBuffers;
		}
	}
}