using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class GraphicsCommandBuffer : CommandBuffer {
		public GraphicsCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, VkCommandBuffer vkCommandBuffer) : base(logicalDevice, commandPool, vkCommandBuffer) { }

		public void CmdBeginPipelineBarrier(VkImage image) {
			VkImageMemoryBarrier2 imageMemoryBarrier2 = new() {
					dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
					dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
					oldLayout = VkImageLayout.ImageLayoutUndefined,
					newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					image = image,
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			VkDependencyInfo dependencyInfo = new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, };
			Vk.CmdPipelineBarrier2(VkCommandBuffer, &dependencyInfo);
		}

		public void CmdEndPipelineBarrier(VkImage image) {
			VkImageMemoryBarrier2 imageMemoryBarrier2 = new() {
					srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
					srcStageMask = VkPipelineStageFlagBits2.PipelineStage2BottomOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
					oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
					image = image,
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			VkDependencyInfo dependencyInfo = new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, };
			Vk.CmdPipelineBarrier2(VkCommandBuffer, &dependencyInfo);
		}

		public void CmdBeginRendering(VkExtent2D extent, VkImageView imageView, VkClearColorValue clearColor) {
			VkRenderingAttachmentInfo vkRenderingAttachmentInfo = new() {
					imageView = imageView,
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() {
							color = clearColor,
							// depthStencil =, TODO look into what this is/how it works/if i want this
					},
			};

			VkRenderingInfo renderingInfo = new() { renderArea = new() { offset = new(0, 0), extent = extent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &vkRenderingAttachmentInfo, };
			Vk.CmdBeginRendering(VkCommandBuffer, &renderingInfo);
		}

		public void CmdEndRendering() => Vk.CmdEndRendering(VkCommandBuffer);

		public void CmdBindGraphicsPipeline(VkPipeline graphicsPipeline) { Vk.CmdBindPipeline(VkCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, graphicsPipeline); }

		public void CmdBindDescriptorSets(VkPipelineLayout graphicsLayout, VkDescriptorSet descriptorSet, VkShaderStageFlagBits shaderStageFlags) {
			VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() { layout = graphicsLayout, descriptorSetCount = 1, pDescriptorSets = &descriptorSet, stageFlags = shaderStageFlags, };
			Vk.CmdBindDescriptorSets2(VkCommandBuffer, &bindDescriptorSetsInfo);
		}

		public void CmdSetViewport(uint x, uint y, uint width, uint height, float minDepth, float maxDepth) {
			VkViewport viewport = new() { x = x, y = y, width = width, height = height, minDepth = minDepth, maxDepth = maxDepth, };
			Vk.CmdSetViewport(VkCommandBuffer, 0, 1, &viewport);
		}

		public void CmdSetScissor(VkExtent2D extent, VkOffset2D offset) {
			VkRect2D scissor = new() { offset = offset, extent = extent, };
			Vk.CmdSetScissor(VkCommandBuffer, 0, 1, &scissor);
		}

		public void CmdBindVertexBuffer(VkBuffer buffer, uint firstBinding, ulong offset = 0) => Vk.CmdBindVertexBuffers(VkCommandBuffer, firstBinding, 1, &buffer, &offset);

		public void CmdBindVertexBuffers(VkBuffer[] buffers, uint firstBinding, ulong[] offsets) {
			fixed (VkBuffer* buffersPtr = buffers) {
				fixed (ulong* offsetsPtr = offsets) { Vk.CmdBindVertexBuffers(VkCommandBuffer, firstBinding, (uint)buffers.Length, buffersPtr, offsetsPtr); }
			}
		}

		public void CmdBindVertexBuffer2(VkBuffer buffer, uint firstBinding, ulong vertexStride, ulong offset = 0) { Vk.CmdBindVertexBuffers2(VkCommandBuffer, firstBinding, 1, &buffer, &offset, null, &vertexStride); }

		public void CmdBindVertexBuffers2(VkBuffer[] buffers, uint firstBinding, ulong[] offsets, ulong[] sizes, ulong[] strides) {
			fixed (VkBuffer* buffersPtr = buffers) {
				fixed (ulong* offsetsPtr = offsets) {
					fixed (ulong* sizesPtr = sizes) {
						fixed (ulong* stridesPtr = strides) { Vk.CmdBindVertexBuffers2(VkCommandBuffer, firstBinding, (uint)buffers.Length, buffersPtr, offsetsPtr, sizesPtr, stridesPtr); }
					}
				}
			}
		}

		public void CmdBindIndexBuffer(VkBuffer buffer, ulong bufferSize, VkIndexType indexType = VkIndexType.IndexTypeUint32, ulong offset = 0) => Vk.CmdBindIndexBuffer2(VkCommandBuffer, buffer, offset, bufferSize, indexType);

		public void CmdDraw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance) => Vk.CmdDraw(VkCommandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
		public void CmdDraw(uint vertexCount) => Vk.CmdDraw(VkCommandBuffer, vertexCount, 1, 0, 0);

		public void CmdDrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance) =>
				Vk.CmdDrawIndexed(VkCommandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

		public void CmdDrawIndexed(uint indexCount) => Vk.CmdDrawIndexed(VkCommandBuffer, indexCount, 1, 0, 0, 0);

		public void SubmitQueue(VkQueue queue, VkSemaphore waitSemaphore, VkSemaphore signalSemaphore, VkFence? fence) {
			VkPipelineStageFlagBits[] waitStages = [ VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, ];
			VkCommandBuffer commandBuffer = VkCommandBuffer;

			fixed (VkPipelineStageFlagBits* waitStagesPtr = waitStages) {
				VkSubmitInfo submitInfo = new() {
						waitSemaphoreCount = 1,
						pWaitSemaphores = &waitSemaphore,
						pWaitDstStageMask = waitStagesPtr,
						commandBufferCount = 1,
						pCommandBuffers = &commandBuffer,
						signalSemaphoreCount = 1,
						pSignalSemaphores = &signalSemaphore,
				};

				VkH.SubmitQueue(queue, submitInfo, fence);
			}
		}
	}
}