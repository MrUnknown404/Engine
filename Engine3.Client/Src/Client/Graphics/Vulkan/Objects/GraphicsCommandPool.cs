using Engine3.Client.Utility.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Client.Graphics.Vulkan.Objects;

public sealed unsafe class GraphicsCommandPool : CommandPool {
	internal GraphicsCommandPool(LogicalGpu logicalGpu, VulkanResourceProvider graphicsResourceProvider, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) : base(logicalGpu, graphicsResourceProvider,
		commandPoolCreateFlags, queueFamilyIndex) { }

	[MustUseReturnValue]
	public GraphicsCommandBuffer[] CreateCommandBuffers(uint count, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
		VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = VkCommandPool, level = level, commandBufferCount = count, };
		VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];
		fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
			VkH.CheckIfSuccess(Vk.AllocateCommandBuffers(LogicalGpu.LogicalDevice, &commandBufferAllocateInfo, commandBuffersPtr), VulkanException.Reason.AllocateCommandBuffers);
		}

		GraphicsCommandBuffer[] buffers = new GraphicsCommandBuffer[count];
		for (int i = 0; i < commandBuffers.Length; i++) {
			GraphicsCommandBuffer commandBuffer = new(LogicalGpu.LogicalDevice, VkCommandPool, commandBuffers[i]);
			buffers[i] = commandBuffer;
			GraphicsResourceProvider.AddCommandBuffer(commandBuffer);
		}

		return buffers;
	}

	[MustUseReturnValue]
	public GraphicsCommandBuffer CreateCommandBuffer(VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
		GraphicsCommandBuffer commandBuffer = new(LogicalGpu.LogicalDevice, VkCommandPool, level);
		GraphicsResourceProvider.AddCommandBuffer(commandBuffer);
		return new GraphicsCommandBuffer(LogicalGpu.LogicalDevice, VkCommandPool, level);
	}
}