using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public abstract unsafe class CommandBuffer : GraphicsResource<CommandBuffer, ulong> {
		public VkCommandPool CommandPool { get; }
		public VkCommandBuffer VkCommandBuffer { get; }

		protected override ulong Handle => VkCommandBuffer.Handle;

		private readonly VkDevice logicalDevice;

		protected CommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBuffer commandBuffer) {
			this.logicalDevice = logicalDevice;
			CommandPool = commandPool;
			VkCommandBuffer = commandBuffer;
		}

		public void ResetCommandBuffer() => Vk.ResetCommandBuffer(VkCommandBuffer, 0);

		public VkResult BeginCommandBuffer(VkCommandBufferUsageFlagBits bufferUsageFlags) {
			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = bufferUsageFlags, };
			return Vk.BeginCommandBuffer(VkCommandBuffer, &commandBufferBeginInfo);
		}

		public VkResult EndCommandBuffer() => Vk.EndCommandBuffer(VkCommandBuffer);

		public void CmdPipelineBarrier(VkDependencyInfo dependencyInfo) => Vk.CmdPipelineBarrier2(VkCommandBuffer, &dependencyInfo);

		[MustUseReturnValue]
		protected static VkCommandBuffer CreateCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = 1, };
			VkCommandBuffer commandBuffers;
			VkH.CheckIfSuccess(Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, &commandBuffers), VulkanException.Reason.AllocateCommandBuffer);
			return commandBuffers;
		}

		protected override void Cleanup() {
			VkCommandBuffer commandBuffers = VkCommandBuffer;
			Vk.FreeCommandBuffers(logicalDevice, CommandPool, 1, &commandBuffers); // TODO move this into CommandPool & make bulk free method
		}
	}
}