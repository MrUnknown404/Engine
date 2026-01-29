using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public unsafe class GraphicsCommandBufferObject : CommandBufferObject {
		public GraphicsCommandBufferObject(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBuffer commandBuffer, VkQueue queue) : base(logicalDevice, commandPool, commandBuffer, queue) { }

		public void CmdBeginRendering(VkExtent2D extent, VkImageView swapChainImageView, VkImageView depthImageView, VkClearColorValue clearColorValue, VkClearDepthStencilValue depthStencilValue) {
			VkRenderingAttachmentInfo colorAttachmentInfo = new() {
					imageView = swapChainImageView,
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() { color = clearColorValue, },
			};

			VkRenderingAttachmentInfo depthAttachmentInfo = new() {
					imageView = depthImageView,
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() { depthStencil = depthStencilValue, },
			};

			VkRenderingInfo renderingInfo = new() {
					renderArea = new() { offset = new(0, 0), extent = extent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &colorAttachmentInfo, pDepthAttachment = &depthAttachmentInfo,
			};

			Vk.CmdBeginRendering(CommandBuffer, &renderingInfo);
		}

		public void CmdEndRendering() => Vk.CmdEndRendering(CommandBuffer);

		public void CmdBindGraphicsPipeline(VkPipeline graphicsPipeline) => Vk.CmdBindPipeline(CommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, graphicsPipeline);

		public void CmdBindDescriptorSet(VkPipelineLayout pipelineLayout, VkDescriptorSet descriptorSet, VkShaderStageFlagBits shaderStageFlags) {
			VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() { layout = pipelineLayout, descriptorSetCount = 1, pDescriptorSets = &descriptorSet, stageFlags = shaderStageFlags, };
			Vk.CmdBindDescriptorSets2(CommandBuffer, &bindDescriptorSetsInfo);
		}

		public void CmdBindDescriptorSets(VkPipelineLayout pipelineLayout, VkDescriptorSet[] descriptorSets, VkShaderStageFlagBits shaderStageFlags) {
			fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
				VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() { layout = pipelineLayout, descriptorSetCount = (uint)descriptorSets.Length, pDescriptorSets = descriptorSetsPtr, stageFlags = shaderStageFlags, };
				Vk.CmdBindDescriptorSets2(CommandBuffer, &bindDescriptorSetsInfo);
			}
		}

		public void CmdSetViewport(uint x, uint y, uint width, uint height, float minDepth, float maxDepth) => CmdSetViewport(new() { x = x, y = y, width = width, height = height, minDepth = minDepth, maxDepth = maxDepth, });
		public void CmdSetViewport(VkViewport viewport) => Vk.CmdSetViewport(CommandBuffer, 0, 1, &viewport);

		public void CmdSetScissor(VkExtent2D extent, VkOffset2D offset) {
			VkRect2D scissor = new() { offset = offset, extent = extent, };
			Vk.CmdSetScissor(CommandBuffer, 0, 1, &scissor);
		}

		public void CmdBindVertexBuffer<T>(T buffer, uint firstBinding, ulong offset = 0) where T : VkBufferObject => CmdBindVertexBuffer(buffer.Buffer, firstBinding, offset);
		public void CmdBindVertexBuffer(VkBuffer buffer, uint firstBinding, ulong offset = 0) => Vk.CmdBindVertexBuffers(CommandBuffer, firstBinding, 1, &buffer, &offset);

		public void CmdBindVertexBuffers(VkBuffer[] buffers, uint firstBinding, ulong[] offsets) {
			fixed (VkBuffer* buffersPtr = buffers) {
				fixed (ulong* offsetsPtr = offsets) { Vk.CmdBindVertexBuffers(CommandBuffer, firstBinding, (uint)buffers.Length, buffersPtr, offsetsPtr); }
			}
		}

		public void CmdBindVertexBuffer2<T>(T buffer, uint firstBinding, ulong vertexStride, ulong offset = 0) where T : VkBufferObject => CmdBindVertexBuffer2(buffer.Buffer, firstBinding, vertexStride, offset);
		public void CmdBindVertexBuffer2(VkBuffer buffer, uint firstBinding, ulong vertexStride, ulong offset = 0) => Vk.CmdBindVertexBuffers2(CommandBuffer, firstBinding, 1, &buffer, &offset, null, &vertexStride);

		public void CmdBindVertexBuffers2(VkBuffer[] buffers, uint firstBinding, ulong[] offsets, ulong[] sizes, ulong[] strides) {
			fixed (VkBuffer* buffersPtr = buffers) {
				fixed (ulong* offsetsPtr = offsets) {
					fixed (ulong* sizesPtr = sizes) {
						fixed (ulong* stridesPtr = strides) { Vk.CmdBindVertexBuffers2(CommandBuffer, firstBinding, (uint)buffers.Length, buffersPtr, offsetsPtr, sizesPtr, stridesPtr); }
					}
				}
			}
		}

		public void CmdBindIndexBuffer<T>(T buffer, ulong bufferSize, VkIndexType indexType = VkIndexType.IndexTypeUint32, ulong offset = 0) where T : VkBufferObject =>
				CmdBindIndexBuffer(buffer.Buffer, bufferSize, indexType, offset);

		public void CmdBindIndexBuffer(VkBuffer buffer, ulong bufferSize, VkIndexType indexType = VkIndexType.IndexTypeUint32, ulong offset = 0) => Vk.CmdBindIndexBuffer2(CommandBuffer, buffer, offset, bufferSize, indexType);

		public void CmdDraw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance) => Vk.CmdDraw(CommandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
		public void CmdDraw(uint vertexCount) => Vk.CmdDraw(CommandBuffer, vertexCount, 1, 0, 0);

		public void CmdDrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance) =>
				Vk.CmdDrawIndexed(CommandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

		public void CmdDrawIndexed(uint indexCount) => Vk.CmdDrawIndexed(CommandBuffer, indexCount, 1, 0, 0, 0);

		public void SubmitQueue(VkSemaphore waitSemaphore, VkSemaphore signalSemaphore, VkFence? fence) {
			VkPipelineStageFlagBits[] waitStages = [ VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, ];
			VkCommandBuffer commandBuffer = CommandBuffer;

			fixed (VkPipelineStageFlagBits* waitStagesPtr = waitStages) {
				VkH.SubmitQueue(Queue,
					new() {
							waitSemaphoreCount = 1,
							pWaitSemaphores = &waitSemaphore,
							pWaitDstStageMask = waitStagesPtr,
							commandBufferCount = 1,
							pCommandBuffers = &commandBuffer,
							signalSemaphoreCount = 1,
							pSignalSemaphores = &signalSemaphore,
					}, fence);
			}
		}
	}
}