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

		protected FrameData[] Frames { get; }

		protected FrameData CurrentFrameData => Frames[CurrentFrame];
		protected VkCommandBuffer CurrentGraphicsCommandBuffer => CurrentFrameData.GraphicsCommandBuffer;
		protected VkSemaphore CurrentImageAvailableSemaphore => CurrentFrameData.ImageAvailableSemaphore;
		protected VkSemaphore CurrentRenderFinishedSemaphore => CurrentFrameData.RenderFinishedSemaphore;
		protected VkFence CurrentInFlightFence => CurrentFrameData.InFlightFence;

		protected PhysicalGpu PhysicalGpu => Window.SelectedGpu;
		protected LogicalGpu LogicalGpu => Window.LogicalGpu;
		protected SwapChain SwapChain => Window.SwapChain;
		protected VkPhysicalDevice PhysicalDevice => PhysicalGpu.PhysicalDevice;
		protected VkDevice LogicalDevice => LogicalGpu.LogicalDevice;

		public override bool IsWindowValid => !Window.WasDestroyed;

		protected byte CurrentFrame { get; private set; }
		protected byte MaxFramesInFlight { get; }

		protected VkRenderer(GameClient gameClient, VkWindow window, VkCommandPool graphicsCommandPool, VkCommandPool transferCommandPool) {
			Window = window;
			MaxFramesInFlight = gameClient.MaxFramesInFlight;
			GraphicsCommandPool = graphicsCommandPool;
			TransferCommandPool = transferCommandPool;

			VkCommandBuffer[] pGraphicsCommandBuffers = VkH.CreateCommandBuffers(LogicalDevice, graphicsCommandPool, MaxFramesInFlight);
			VkSemaphore[] pImageAvailableSemaphores = VkH.CreateSemaphores(LogicalDevice, MaxFramesInFlight);
			VkSemaphore[] pRenderFinishedSemaphores = VkH.CreateSemaphores(LogicalDevice, MaxFramesInFlight);
			VkFence[] pInFlightFences = VkH.CreateFences(LogicalDevice, MaxFramesInFlight);

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(LogicalDevice, pGraphicsCommandBuffers[i], pImageAvailableSemaphores[i], pRenderFinishedSemaphores[i], pInFlightFences[i]); }
		}

		protected abstract void DrawFrame(VkPipeline graphicsPipeline, VkCommandBuffer graphicsCommandBuffer, float delta);

		protected virtual void UpdateUniformBuffer(float delta) { }

		protected unsafe bool BeginFrame(VkCommandBuffer graphicsCommandBuffer, out uint swapChainImageIndex) {
			VkFence currentFence = CurrentInFlightFence;

			// not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			// vulkan-tutorial.com says wait for all
			// vkguide.dev says wait for current
			Vk.WaitForFences(LogicalDevice, 1, &currentFence, (int)Vk.True, ulong.MaxValue);

			uint tempSwapChainImageIndex;
			VkResult vkResult = Vk.AcquireNextImageKHR(LogicalDevice, SwapChain.VkSwapChain, ulong.MaxValue, CurrentImageAvailableSemaphore, VkFence.Zero, &tempSwapChainImageIndex); // TODO 2
			swapChainImageIndex = tempSwapChainImageIndex; // ew

			if (vkResult == VkResult.ErrorOutOfDateKhr) {
				SwapChain.Recreate();
				return false;
			} else if (vkResult is not VkResult.Success and not VkResult.SuboptimalKhr) { throw new VulkanException("Failed to acquire swap chain image"); }

			Vk.ResetFences(LogicalDevice, 1, &currentFence);

			Vk.ResetCommandBuffer(graphicsCommandBuffer, 0);

			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = 0, pInheritanceInfo = null, };
			if (Vk.BeginCommandBuffer(graphicsCommandBuffer, &commandBufferBeginInfo) != VkResult.Success) { throw new VulkanException("Failed to begin recording command buffer"); }

			VkImageMemoryBarrier imageMemoryBarrier = new() {
					dstAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutUndefined,
					newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					image = SwapChain.Images[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(graphicsCommandBuffer, VkPipelineStageFlagBits.PipelineStageTopOfPipeBit, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, 0, 0, null, 0, null, 1, &imageMemoryBarrier); // TODO 2

			Color4<Rgba> clearColor = Window.ClearColor;
			VkClearColorValue clearColorValue = new();
			clearColorValue.float32[0] = clearColor.X;
			clearColorValue.float32[1] = clearColor.Y;
			clearColorValue.float32[2] = clearColor.Z;
			clearColorValue.float32[3] = clearColor.W;

			VkRenderingAttachmentInfo vkRenderingAttachmentInfo = new() {
					imageView = SwapChain.ImageViews[swapChainImageIndex],
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() {
							color = clearColorValue,
							// depthStencil =, TODO look into what this is/how it works/if i want this
					},
			};

			VkRenderingInfo renderingInfo = new() { renderArea = new() { offset = new(0, 0), extent = SwapChain.Extent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &vkRenderingAttachmentInfo, };

			Vk.CmdBeginRendering(graphicsCommandBuffer, &renderingInfo);

			return true;
		}

		protected unsafe void EndFrame(VkCommandBuffer graphicsCommandBuffer, uint swapChainImageIndex) {
			Vk.CmdEndRendering(graphicsCommandBuffer);

			VkImageMemoryBarrier imageMemoryBarrier = new() {
					srcAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
					image = SwapChain.Images[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(graphicsCommandBuffer, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, VkPipelineStageFlagBits.PipelineStageBottomOfPipeBit, 0, 0, null, 0, null, 1, &imageMemoryBarrier); // TODO 2

			if (Vk.EndCommandBuffer(graphicsCommandBuffer) != VkResult.Success) { throw new VulkanException("Failed to end recording command buffer"); }

			VkH.SubmitCommandBufferQueue(LogicalGpu.GraphicsQueue, graphicsCommandBuffer, CurrentImageAvailableSemaphore, CurrentRenderFinishedSemaphore, CurrentInFlightFence);
		}

		protected unsafe void PresentFrame(uint swapChainImageIndex) {
			VkSwapchainKHR swapChain = SwapChain.VkSwapChain;
			VkSemaphore renderFinishedSemaphore = CurrentRenderFinishedSemaphore;
			VkPresentInfoKHR presentInfo = new() { waitSemaphoreCount = 1, pWaitSemaphores = &renderFinishedSemaphore, swapchainCount = 1, pSwapchains = &swapChain, pImageIndices = &swapChainImageIndex, };

			VkResult vkResult = Vk.QueuePresentKHR(LogicalGpu.PresentQueue, &presentInfo);
			if (vkResult is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr || Window.WasResized) {
				Window.WasResized = false;
				SwapChain.Recreate();
			} else if (vkResult != VkResult.Success) { throw new VulkanException("Failed to present swap chain image"); }

			CurrentFrame = (byte)((CurrentFrame + 1) % MaxFramesInFlight);
		}

		protected override unsafe void Cleanup() {
			Vk.DestroyCommandPool(LogicalDevice, TransferCommandPool, null);
			Vk.DestroyCommandPool(LogicalDevice, GraphicsCommandPool, null);

			foreach (FrameData frame in Frames) { frame.Destroy(); }
		}

		[Obsolete("create a proper way of creating shaders & pipelines")] // TODO move
		protected static unsafe void CreateGraphicsPipeline(VkDevice logicalDevice, VkFormat swapChainImageFormat, VkDescriptorSetLayout[]? descriptorSetLayouts, string shaderName, ShaderLanguage shaderLanguage, Assembly assembly,
			out VkPipeline graphicsPipeline, out VkPipelineLayout pipelineLayout) {
			VkShaderModule? vertShaderModule = VkH.CreateShaderModule(logicalDevice, shaderName, shaderLanguage, ShaderType.Vertex, assembly);
			VkShaderModule? fragShaderModule = VkH.CreateShaderModule(logicalDevice, shaderName, shaderLanguage, ShaderType.Fragment, assembly);
			if (vertShaderModule == null || fragShaderModule == null) { throw new VulkanException("Failed to create shader modules"); }

			byte* entryPointPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("main"u8));
			VkPipelineShaderStageCreateInfo vertShaderStageCreateInfo = new() { stage = VkShaderStageFlagBits.ShaderStageVertexBit, module = vertShaderModule.Value, pName = entryPointPtr, };
			VkPipelineShaderStageCreateInfo fragShaderStageCreateInfo = new() { stage = VkShaderStageFlagBits.ShaderStageFragmentBit, module = fragShaderModule.Value, pName = entryPointPtr, };

			VkH.CreateGraphicsPipeline(logicalDevice, swapChainImageFormat, [ vertShaderStageCreateInfo, fragShaderStageCreateInfo, ], descriptorSetLayouts, out graphicsPipeline, out pipelineLayout);

			Vk.DestroyShaderModule(logicalDevice, fragShaderModule.Value, null);
			Vk.DestroyShaderModule(logicalDevice, vertShaderModule.Value, null);
		}

		protected unsafe class FrameData {
			public VkCommandBuffer GraphicsCommandBuffer { get; }
			public VkSemaphore ImageAvailableSemaphore { get; }
			public VkSemaphore RenderFinishedSemaphore { get; }
			public VkFence InFlightFence { get; }

			private readonly VkDevice logicalDevice;

			public FrameData(VkDevice logicalDevice, VkCommandBuffer graphicsCommandBuffer, VkSemaphore imageAvailableSemaphore, VkSemaphore renderFinishedSemaphore, VkFence inFlightFence) {
				GraphicsCommandBuffer = graphicsCommandBuffer;
				ImageAvailableSemaphore = imageAvailableSemaphore;
				RenderFinishedSemaphore = renderFinishedSemaphore;
				InFlightFence = inFlightFence;
				this.logicalDevice = logicalDevice;
			}

			public void Destroy() {
				Vk.DestroySemaphore(logicalDevice, ImageAvailableSemaphore, null);
				Vk.DestroySemaphore(logicalDevice, RenderFinishedSemaphore, null);
				Vk.DestroyFence(logicalDevice, InFlightFence, null);
			}
		}
	}
}