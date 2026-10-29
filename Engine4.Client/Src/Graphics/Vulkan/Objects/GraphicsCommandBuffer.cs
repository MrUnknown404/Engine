using Engine4.Client.Graphics._Test;
using Engine4.Client.Utility.Extensions;
using Engine4.Utility.Math;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class GraphicsCommandBuffer : CommandBuffer {
	internal GraphicsCommandBuffer(string debugName, LogicalGpu logicalGpu, VkCommandPool vkCommandPool, VkCommandBuffer commandBuffer) : base(debugName, logicalGpu, vkCommandPool, commandBuffer) { }

	// begin/end rendering
	public void CmdBeginRendering(VkExtent2D extent, VkImageView colorImageView, Color4 clearColor, VkImageView? depthImageView, VkClearDepthStencilValue? depthStencil) {
		VkRenderingAttachmentInfo colorAttachmentInfo = new() {
				imageView = colorImageView,
				imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
				loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
				storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
				clearValue = new() { color = clearColor.ToVkClearColorValue(), },
		};

		VkRenderingAttachmentInfo depthAttachmentInfo;
		if (depthImageView != null && depthStencil != null) {
			depthAttachmentInfo = new() {
					imageView = depthImageView.Value,
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() { depthStencil = depthStencil.Value, },
			};
		}

		VkRenderingInfo renderingInfo = new() {
				renderArea = new() { offset = new(0, 0), extent = extent, }, //
				layerCount = 1,
				colorAttachmentCount = 1,
				pColorAttachments = &colorAttachmentInfo,
				pDepthAttachment = depthImageView != null ? &depthAttachmentInfo : null,
		};

		CmdBeginRendering(renderingInfo);
	}

	public void CmdBeginRendering(VkRenderingInfo renderingInfo) => Vk.CmdBeginRendering(VkCommandBuffer, &renderingInfo);

	public void CmdEndRendering() => Vk.CmdEndRendering(VkCommandBuffer);

	// set viewports
	public void CmdSetViewport(float x, float y, float width, float height, float minDepth, float maxDepth, uint firstViewport = 0) =>
			CmdSetViewport(new() { x = x, y = y, width = width, height = height, minDepth = minDepth, maxDepth = maxDepth, }, firstViewport);

	public void CmdSetViewport(VkViewport viewport, uint firstViewport = 0) => Vk.CmdSetViewport(VkCommandBuffer, firstViewport, 1, &viewport);

	public void CmdSetViewports(VkViewport[] viewports, uint firstViewport = 0) {
		fixed (VkViewport* viewportsPtr = viewports) { Vk.CmdSetViewport(VkCommandBuffer, firstViewport, (uint)viewports.Length, viewportsPtr); }
	}

	// set scissors
	public void CmdSetScissor(int x, int y, uint width, uint height, uint firstScissor = 0) => CmdSetScissor(new() { offset = new(x, y), extent = new(width, height), }, firstScissor);
	public void CmdSetScissor(VkOffset2D offset, VkExtent2D extent, uint firstScissor = 0) => CmdSetScissor(new() { offset = offset, extent = extent, }, firstScissor);
	public void CmdSetScissor(VkRect2D scissor, uint firstScissor = 0) => Vk.CmdSetScissor(VkCommandBuffer, firstScissor, 1, &scissor);

	public void CmdSetScissors(VkRect2D[] scissors, uint firstScissor = 0) {
		fixed (VkRect2D* scissorPtr = scissors) { Vk.CmdSetScissor(VkCommandBuffer, firstScissor, (uint)scissors.Length, scissorPtr); }
	}

	// push constants
	[Obsolete]
	public void CmdPushConstants<T>(VkPipelineLayout pipelineLayout, VkShaderStageFlagBits shaderStageFlags, T data, uint offset = 0) where T : unmanaged =>
			Vk.CmdPushConstants(VkCommandBuffer, pipelineLayout, shaderStageFlags, offset, (uint)sizeof(T), &data);

	// push data
	public void CmdPushData<T>(T data, uint offset = 0) where T : unmanaged {
		VkPushDataInfoEXT pushDataInfo = new() { data = new() { address = &data, size = (nuint)sizeof(T), }, offset = offset, };
		Vk.CmdPushDataEXT(VkCommandBuffer, &pushDataInfo);
	}

	// bind pipeline
	public void CmdBindGraphicsPipeline(GraphicsPipeline graphicsPipeline) => Vk.CmdBindPipeline(VkCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, graphicsPipeline.Pipeline);

	// bind descriptor sets
	[Obsolete]
	public void CmdBindDescriptorSet(VkPipelineLayout pipelineLayout, VkDescriptorSet descriptorSet, VkShaderStageFlagBits shaderStageFlags, uint firstSet = 0) {
		VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() { layout = pipelineLayout, descriptorSetCount = 1, pDescriptorSets = &descriptorSet, firstSet = firstSet, stageFlags = shaderStageFlags, };
		Vk.CmdBindDescriptorSets2(VkCommandBuffer, &bindDescriptorSetsInfo);
	}

	[Obsolete]
	public void CmdBindDescriptorSets(VkPipelineLayout pipelineLayout, VkDescriptorSet[] descriptorSets, VkShaderStageFlagBits shaderStageFlags, uint firstSet = 0) {
		fixed (VkDescriptorSet* pDescriptorSets = descriptorSets) {
			VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() {
					layout = pipelineLayout, descriptorSetCount = (uint)descriptorSets.Length, pDescriptorSets = pDescriptorSets, firstSet = firstSet, stageFlags = shaderStageFlags,
			};

			Vk.CmdBindDescriptorSets2(VkCommandBuffer, &bindDescriptorSetsInfo);
		}
	}

	public void CmdBindSamplerHeap(SamplerDescriptors samplerDescriptors) {
		VkBindHeapInfoEXT bindHeapInfo = new() {
				heapRange = new() {
						address = samplerDescriptors.DeviceAddress, //
						size = samplerDescriptors.SamplerHeapBuffer.Size,
				},
				reservedRangeOffset = samplerDescriptors.SamplerHeapBuffer.Size - samplerDescriptors.MinHeapReservedRange,
				reservedRangeSize = samplerDescriptors.MinHeapReservedRange,
		};

		Vk.CmdBindSamplerHeapEXT(VkCommandBuffer, &bindHeapInfo);
	}

	public void CmdBindResourceHeap(ResourceDescriptors resourceDescriptors) {
		VkBindHeapInfoEXT bindHeapInfo = new() {
				heapRange = new() {
						address = resourceDescriptors.DeviceAddress, //
						size = resourceDescriptors.ResourceHeapBuffer.Size,
				},
				reservedRangeOffset = resourceDescriptors.ResourceHeapBuffer.Size - resourceDescriptors.MinHeapReservedRange,
				reservedRangeSize = resourceDescriptors.MinHeapReservedRange,
		};

		Vk.CmdBindResourceHeapEXT(VkCommandBuffer, &bindHeapInfo);
	}

	// bind vertex
	public void CmdBindVertexBuffer(VulkanBuffer buffer, uint firstBinding, ulong offset = 0) {
		VkBuffer vkBuffer = buffer.VkBuffer;
		Vk.CmdBindVertexBuffers(VkCommandBuffer, firstBinding, 1, &vkBuffer, &offset);
	}

	public void CmdBindVertexBuffer2(VulkanBuffer buffer, uint firstBinding, ulong vertexStride, ulong offset = 0) {
		VkBuffer vkBuffer = buffer.VkBuffer;
		Vk.CmdBindVertexBuffers2(VkCommandBuffer, firstBinding, 1, &vkBuffer, &offset, null, &vertexStride);
	}

	// bind index
	public void CmdBindIndexBuffer(VulkanBuffer buffer, VkIndexType indexType = VkIndexType.IndexTypeUint32, ulong offset = 0) => Vk.CmdBindIndexBuffer(VkCommandBuffer, buffer.VkBuffer, offset, indexType);

	public void CmdBindIndexBuffer2(VulkanBuffer buffer, ulong bufferSize, VkIndexType indexType = VkIndexType.IndexTypeUint32, ulong offset = 0) =>
			Vk.CmdBindIndexBuffer2(VkCommandBuffer, buffer.VkBuffer, offset, bufferSize, indexType);

	// draw commands
	public void CmdDrawIndexed(uint indexCount) => CmdDrawIndexed(indexCount, 1, 0, 0, 0);

	public void CmdDrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance) =>
			Vk.CmdDrawIndexed(VkCommandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

	public void CmdDrawIndirect(VkBuffer buffer, ulong offset, uint drawCount, uint stride) => Vk.CmdDrawIndirect(VkCommandBuffer, buffer, offset, drawCount, stride);
	public void CmdDrawIndexedIndirect(VkBuffer buffer, ulong offset, uint drawCount, uint stride) => Vk.CmdDrawIndexedIndirect(VkCommandBuffer, buffer, offset, drawCount, stride);
}