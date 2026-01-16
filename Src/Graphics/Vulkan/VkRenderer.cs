using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;

namespace Engine3.Graphics.Vulkan {
	public abstract class VkRenderer : Renderer {
		protected VkWindow Window { get; }

		protected VkCommandPool GraphicsCommandPool { get; }
		protected VkCommandPool TransferCommandPool { get; }

		protected VkCommandBuffer[] GraphicsCommandBuffers { get; } // TODO should these be wrapped into their own class?
		protected VkSemaphore[] ImageAvailableSemaphores { get; }
		protected VkSemaphore[] RenderFinishedSemaphores { get; }
		protected VkFence[] InFlightFences { get; }

		protected VkCommandBuffer CurrentGraphicsCommandBuffer => GraphicsCommandBuffers[currentFrame];
		protected VkSemaphore CurrentImageAvailableSemaphore => ImageAvailableSemaphores[currentFrame];
		protected VkSemaphore CurrentRenderFinishedSemaphore => RenderFinishedSemaphores[currentFrame];
		protected VkFence CurrentInFlightFences => InFlightFences[currentFrame];

		protected VkPhysicalDevice PhysicalDevice => Window.SelectedGpu.VkPhysicalDevice;
		protected VkDevice LogicalDevice => Window.LogicalGpu.VkLogicalDevice;
		protected SwapChain SwapChain => Window.SwapChain;

		public override bool IsWindowValid => !Window.WasDestroyed;

		private readonly uint maxFramesInFlight;
		private uint currentFrame;

		protected VkRenderer(GameClient gameClient, VkWindow window, VkCommandPool graphicsCommandPool, VkCommandPool transferCommandPool, VkCommandBuffer[] graphicsCommandBuffers, VkSemaphore[] imageAvailableSemaphores,
			VkSemaphore[] renderFinishedSemaphores, VkFence[] inFlightFences) {
			Window = window;
			maxFramesInFlight = gameClient.MaxFramesInFlight;
			GraphicsCommandPool = graphicsCommandPool;
			TransferCommandPool = transferCommandPool;
			GraphicsCommandBuffers = graphicsCommandBuffers;
			ImageAvailableSemaphores = imageAvailableSemaphores;
			RenderFinishedSemaphores = renderFinishedSemaphores;
			InFlightFences = inFlightFences;
		}

		protected abstract void DrawFrame(VkPipeline vkGraphicsPipeline, VkCommandBuffer vkCommandBuffer, VkExtent2D vkExtent, float delta);

		protected override unsafe void Cleanup() {
			Vk.DestroyCommandPool(LogicalDevice, TransferCommandPool, null);
			Vk.DestroyCommandPool(LogicalDevice, GraphicsCommandPool, null);

			foreach (VkSemaphore vkSemaphore in ImageAvailableSemaphores) { Vk.DestroySemaphore(LogicalDevice, vkSemaphore, null); }
			foreach (VkSemaphore vkSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(LogicalDevice, vkSemaphore, null); }
			foreach (VkFence vkFence in InFlightFences) { Vk.DestroyFence(LogicalDevice, vkFence, null); }
		}

		protected unsafe bool BeginFrame(VkCommandBuffer vkCommandBuffer, out uint swapChainImageIndex) {
			fixed (VkFence* inFlightPtr = InFlightFences) {
				Vk.WaitForFences(LogicalDevice, 1, &inFlightPtr[currentFrame], (int)Vk.True, ulong.MaxValue);

				uint tempSwapChainImageIndex;
				VkResult vkResult = Vk.AcquireNextImageKHR(LogicalDevice, SwapChain.VkSwapChain, ulong.MaxValue, CurrentImageAvailableSemaphore, VkFence.Zero, &tempSwapChainImageIndex); // TODO 2
				swapChainImageIndex = tempSwapChainImageIndex; // ew

				if (vkResult == VkResult.ErrorOutOfDateKhr) {
					SwapChain.Recreate(Window);
					return false;
				} else if (vkResult is not VkResult.Success and not VkResult.SuboptimalKhr) { throw new VulkanException("Failed to acquire swap chain image"); }

				Vk.ResetFences(LogicalDevice, 1, &inFlightPtr[currentFrame]);
			}

			Vk.ResetCommandBuffer(vkCommandBuffer, 0);

			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = 0, pInheritanceInfo = null, };
			if (Vk.BeginCommandBuffer(vkCommandBuffer, &commandBufferBeginInfo) != VkResult.Success) { throw new VulkanException("Failed to begin recording command buffer"); }

			VkImageMemoryBarrier vkImageMemoryBarrier = new() {
					dstAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutUndefined,
					newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					image = SwapChain.VkImages[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(vkCommandBuffer, VkPipelineStageFlagBits.PipelineStageTopOfPipeBit, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, 0, 0, null, 0, null, 1, &vkImageMemoryBarrier); // TODO use 2?

			Color4<Rgba> clearColor = Window.ClearColor;
			VkClearColorValue vkClearColor = new();
			vkClearColor.float32[0] = clearColor.X;
			vkClearColor.float32[1] = clearColor.Y;
			vkClearColor.float32[2] = clearColor.Z;
			vkClearColor.float32[3] = clearColor.W;

			VkRenderingAttachmentInfo vkRenderingAttachmentInfo = new() {
					imageView = SwapChain.VkImageViews[swapChainImageIndex],
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() {
							color = vkClearColor,
							// depthStencil =, TODO look into what this is/how it works/if i want this
					},
			};

			VkRenderingInfo vkRenderingInfo = new() { renderArea = new() { offset = new(0, 0), extent = SwapChain.VkExtent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &vkRenderingAttachmentInfo, };

			Vk.CmdBeginRendering(vkCommandBuffer, &vkRenderingInfo);

			return true;
		}

		protected unsafe void EndFrame(VkCommandBuffer vkCommandBuffer, uint swapChainImageIndex) {
			Vk.CmdEndRendering(vkCommandBuffer);

			VkImageMemoryBarrier vkImageMemoryBarrier = new() {
					srcAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
					image = SwapChain.VkImages[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(vkCommandBuffer, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, VkPipelineStageFlagBits.PipelineStageBottomOfPipeBit, 0, 0, null, 0, null, 1, &vkImageMemoryBarrier); // TODO use 2?

			if (Vk.EndCommandBuffer(vkCommandBuffer) != VkResult.Success) { throw new VulkanException("Failed to end recording command buffer"); }

			VkH.SubmitCommandBufferQueue(Window.LogicalGpu.VkGraphicsQueue, vkCommandBuffer, CurrentImageAvailableSemaphore, CurrentRenderFinishedSemaphore, CurrentInFlightFences);
		}

		protected unsafe void PresentFrame(uint swapChainImageIndex) {
			VkSwapchainKHR vkSwapchain = SwapChain.VkSwapChain;
			VkSemaphore renderFinishedSemaphore = CurrentRenderFinishedSemaphore;
			VkPresentInfoKHR presentInfo = new() { waitSemaphoreCount = 1, pWaitSemaphores = &renderFinishedSemaphore, swapchainCount = 1, pSwapchains = &vkSwapchain, pImageIndices = &swapChainImageIndex, };

			VkResult vkResult = Vk.QueuePresentKHR(Window.LogicalGpu.VkPresentQueue, &presentInfo);
			if (vkResult is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr || Window.WasResized) {
				Window.WasResized = false;
				SwapChain.Recreate(Window);
			} else if (vkResult != VkResult.Success) { throw new VulkanException("Failed to present swap chain image"); }

			currentFrame = (currentFrame + 1) % maxFramesInFlight;
		}

		protected static unsafe void CreateGraphicsPipeline(VkDevice vkLogicalDevice, VkFormat swapChainImageFormat, Assembly assembly, out VkPipeline vkGraphicsPipeline, out VkPipelineLayout vkPipelineLayout) { // TODO move
			VkShaderModule? vertShaderModule = VkH.CreateShaderModule(vkLogicalDevice, "Test", ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VkShaderModule? fragShaderModule = VkH.CreateShaderModule(vkLogicalDevice, "Test", ShaderLanguage.Glsl, ShaderType.Fragment, assembly);
			if (vertShaderModule == null || fragShaderModule == null) { throw new VulkanException("Failed to create shader modules"); }

			byte* entryPointPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("main"u8));
			VkPipelineShaderStageCreateInfo vertShaderStageCreateInfo = new() { stage = VkShaderStageFlagBits.ShaderStageVertexBit, module = vertShaderModule.Value, pName = entryPointPtr, };
			VkPipelineShaderStageCreateInfo fragShaderStageCreateInfo = new() { stage = VkShaderStageFlagBits.ShaderStageFragmentBit, module = fragShaderModule.Value, pName = entryPointPtr, };

			VkH.CreateGraphicsPipeline(vkLogicalDevice, swapChainImageFormat, [ vertShaderStageCreateInfo, fragShaderStageCreateInfo, ], out vkGraphicsPipeline, out vkPipelineLayout);

			Vk.DestroyShaderModule(vkLogicalDevice, fragShaderModule.Value, null);
			Vk.DestroyShaderModule(vkLogicalDevice, vertShaderModule.Value, null);
		}

		protected static unsafe void CreateVertexBuffer(VkPhysicalDevice vkPhysicalDevice, VkDevice vkLogicalDevice, VkMemoryPropertyFlagBits vkMemoryPropertyFlag, TestVertex[] vertices, out VkBuffer vertexBuffer,
			out VkDeviceMemory vertexBufferDeviceMemory) { // TODO move
			ulong bufferSize = (ulong)(sizeof(TestVertex) * vertices.Length);

			vertexBuffer = VkH.CreateVertexBuffer(vkLogicalDevice, bufferSize);
			vertexBufferDeviceMemory = VkH.CreateDeviceMemory(vkPhysicalDevice, vkLogicalDevice, vertexBuffer, vkMemoryPropertyFlag);

			Vk.BindBufferMemory(vkLogicalDevice, vertexBuffer, vertexBufferDeviceMemory, 0);

			fixed (TestVertex* verticesPtr = vertices) {
				void* data;
				Vk.MapMemory(vkLogicalDevice, vertexBufferDeviceMemory, 0, bufferSize, 0, &data); // TODO 2
				Buffer.MemoryCopy(verticesPtr, data, bufferSize, bufferSize);
				Vk.UnmapMemory(vkLogicalDevice, vertexBufferDeviceMemory);
			}
		}
	}
}