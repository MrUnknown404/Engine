using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class GraphicsCommandBuffer : CommandBuffer {
		internal GraphicsCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBuffer commandBuffer) : base(logicalDevice, commandPool, commandBuffer) => PrintCreate();

		internal GraphicsCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) : base(logicalDevice, commandPool,
			CreateCommandBuffer(logicalDevice, commandPool, level)) =>
				PrintCreate();

		public void CmdBeginRendering(VkExtent2D extent, VkImageView swapChainImageView, VkImageView? depthImageView, VkClearColorValue clearColorValue, VkClearDepthStencilValue depthStencilValue) {
			VkRenderingAttachmentInfo colorAttachmentInfo = new() {
					imageView = swapChainImageView,
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() { color = clearColorValue, },
			};

			VkRenderingInfo renderingInfo = new() { renderArea = new() { offset = new(0, 0), extent = extent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &colorAttachmentInfo, };

			if (depthImageView != null) {
				VkRenderingAttachmentInfo depthAttachmentInfo = new() {
						imageView = depthImageView.Value,
						imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
						loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
						storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
						clearValue = new() { depthStencil = depthStencilValue, },
				};

				renderingInfo.pDepthAttachment = &depthAttachmentInfo;
			}

			Vk.CmdBeginRendering(VkCommandBuffer, &renderingInfo);
		}

		public void CmdEndRendering() => Vk.CmdEndRendering(VkCommandBuffer);

		public void CmdBindGraphicsPipeline(VkPipeline graphicsPipeline) => Vk.CmdBindPipeline(VkCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, graphicsPipeline);

		public void CmdBindDescriptorSet(VkPipelineLayout pipelineLayout, VkDescriptorSet descriptorSet, VkShaderStageFlagBits shaderStageFlags) {
			VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() { layout = pipelineLayout, descriptorSetCount = 1, pDescriptorSets = &descriptorSet, stageFlags = shaderStageFlags, };
			Vk.CmdBindDescriptorSets2(VkCommandBuffer, &bindDescriptorSetsInfo);
		}

		public void CmdBindDescriptorSets(VkPipelineLayout pipelineLayout, VkDescriptorSet[] descriptorSets, VkShaderStageFlagBits shaderStageFlags) {
			fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
				VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() { layout = pipelineLayout, descriptorSetCount = (uint)descriptorSets.Length, pDescriptorSets = descriptorSetsPtr, stageFlags = shaderStageFlags, };
				Vk.CmdBindDescriptorSets2(VkCommandBuffer, &bindDescriptorSetsInfo);
			}
		}

		public void CmdPushConstants<T>(VkPipelineLayout pipelineLayout, VkShaderStageFlagBits shaderStageFlags, uint offset, T data) where T : unmanaged =>
				Vk.CmdPushConstants(VkCommandBuffer, pipelineLayout, shaderStageFlags, offset, (uint)sizeof(T), &data);

		public void CmdSetViewport(uint x, uint y, uint width, uint height, float minDepth, float maxDepth) => CmdSetViewport(new() { x = x, y = y, width = width, height = height, minDepth = minDepth, maxDepth = maxDepth, });
		public void CmdSetViewport(VkViewport viewport) => Vk.CmdSetViewport(VkCommandBuffer, 0, 1, &viewport);

		public void CmdSetScissor(VkOffset2D offset, VkExtent2D extent) {
			VkRect2D scissor = new() { offset = offset, extent = extent, };
			Vk.CmdSetScissor(VkCommandBuffer, 0, 1, &scissor);
		}

		public void CmdBindVertexBuffer(VulkanBuffer buffer, uint firstBinding, ulong offset = 0) => CmdBindVertexBuffer(buffer.Buffer, firstBinding, offset);
		public void CmdBindVertexBuffer(VkBuffer buffer, uint firstBinding, ulong offset = 0) => Vk.CmdBindVertexBuffers(VkCommandBuffer, firstBinding, 1, &buffer, &offset);

		public void CmdBindVertexBuffers(VkBuffer[] buffers, uint firstBinding, ulong[] offsets) {
			fixed (VkBuffer* buffersPtr = buffers) {
				fixed (ulong* offsetsPtr = offsets) { Vk.CmdBindVertexBuffers(VkCommandBuffer, firstBinding, (uint)buffers.Length, buffersPtr, offsetsPtr); }
			}
		}

		public void CmdBindVertexBuffer2(VulkanBuffer buffer, uint firstBinding, ulong vertexStride, ulong offset = 0) => CmdBindVertexBuffer2(buffer.Buffer, firstBinding, vertexStride, offset);
		public void CmdBindVertexBuffer2(VkBuffer buffer, uint firstBinding, ulong vertexStride, ulong offset = 0) => Vk.CmdBindVertexBuffers2(VkCommandBuffer, firstBinding, 1, &buffer, &offset, null, &vertexStride);

		public void CmdBindVertexBuffers2(VkBuffer[] buffers, uint firstBinding, ulong[] offsets, ulong[] sizes, ulong[] strides) {
			fixed (VkBuffer* buffersPtr = buffers) {
				fixed (ulong* offsetsPtr = offsets) {
					fixed (ulong* sizesPtr = sizes) {
						fixed (ulong* stridesPtr = strides) { Vk.CmdBindVertexBuffers2(VkCommandBuffer, firstBinding, (uint)buffers.Length, buffersPtr, offsetsPtr, sizesPtr, stridesPtr); }
					}
				}
			}
		}

		public void CmdBindIndexBuffer(VulkanBuffer buffer, ulong bufferSize, VkIndexType indexType = VkIndexType.IndexTypeUint32, ulong offset = 0) => CmdBindIndexBuffer(buffer.Buffer, bufferSize, indexType, offset);
		public void CmdBindIndexBuffer(VkBuffer buffer, ulong bufferSize, VkIndexType indexType = VkIndexType.IndexTypeUint32, ulong offset = 0) => Vk.CmdBindIndexBuffer2(VkCommandBuffer, buffer, offset, bufferSize, indexType);

		public void CmdDraw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance) => Vk.CmdDraw(VkCommandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
		public void CmdDraw(uint vertexCount) => Vk.CmdDraw(VkCommandBuffer, vertexCount, 1, 0, 0);

		public void CmdDrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance) =>
				Vk.CmdDrawIndexed(VkCommandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

		public void CmdDrawIndexed(uint indexCount) => CmdDrawIndexed(indexCount, 1, 0, 0, 0);
	}
}