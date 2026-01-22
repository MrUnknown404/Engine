using Engine3.Exceptions;
using Engine3.Utils.Extensions;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public abstract class VkRenderer : Renderer {
		protected VkWindow Window { get; }

		protected VkCommandPool GraphicsCommandPool { get; }
		protected VkCommandPool TransferCommandPool { get; }

		protected FrameData[] Frames { get; }
		public VkSemaphore[] RenderFinishedSemaphores { get; }

		protected FrameData CurrentFrameData => Frames[CurrentFrame];
		protected VkCommandBuffer CurrentGraphicsCommandBuffer => CurrentFrameData.GraphicsCommandBuffer;
		protected VkSemaphore CurrentImageAvailableSemaphore => CurrentFrameData.ImageAvailableSemaphore;
		protected VkFence CurrentInFlightFence => CurrentFrameData.InFlightFence;

		protected PhysicalGpu PhysicalGpu => Window.SelectedGpu;
		protected LogicalGpu LogicalGpu => Window.LogicalGpu;
		protected SwapChain SwapChain => Window.SwapChain;
		protected VkPhysicalDevice PhysicalDevice => PhysicalGpu.PhysicalDevice;
		protected VkDevice LogicalDevice => LogicalGpu.LogicalDevice;

		public override bool IsWindowValid => !Window.WasDestroyed;

		protected byte CurrentFrame { get; private set; }
		protected byte MaxFramesInFlight { get; }

		protected VkRenderer(VkWindow window, byte maxFramesInFlight) {
			Window = window;
			MaxFramesInFlight = maxFramesInFlight;
			GraphicsCommandPool = VkH.CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, PhysicalGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = VkH.CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, PhysicalGpu.QueueFamilyIndices.TransferFamily);
			RenderFinishedSemaphores = VkH.CreateSemaphores(LogicalDevice, (uint)SwapChain.Images.Length);

			VkCommandBuffer[] graphicsCommandBuffers = VkH.CreateCommandBuffers(LogicalDevice, GraphicsCommandPool, MaxFramesInFlight);
			VkSemaphore[] imageAvailableSemaphores = VkH.CreateSemaphores(LogicalDevice, MaxFramesInFlight);
			VkFence[] inFlightFences = VkH.CreateFences(LogicalDevice, MaxFramesInFlight);

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(LogicalDevice, graphicsCommandBuffers[i], imageAvailableSemaphores[i], inFlightFences[i]); }
		}

		protected virtual void UpdateUniformBuffer(float delta) { }

		protected unsafe bool AcquireNextImage(out uint swapChainImageIndex) {
			VkFence currentFence = CurrentInFlightFence;

			// not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			// vulkan-tutorial.com waits for all
			// vkguide.dev waits for current
			Vk.WaitForFences(LogicalDevice, 1, &currentFence, (int)Vk.True, ulong.MaxValue);

			uint tempSwapChainImageIndex;
			VkResult vkResult = Vk.AcquireNextImageKHR(LogicalDevice, SwapChain.VkSwapChain, ulong.MaxValue, CurrentImageAvailableSemaphore, VkFence.Zero, &tempSwapChainImageIndex);
			swapChainImageIndex = tempSwapChainImageIndex;

			if (vkResult == VkResult.ErrorOutOfDateKhr) {
				SwapChain.Recreate();
				return false;
			} else if (vkResult is not VkResult.Success and not VkResult.SuboptimalKhr) { throw new VulkanException("Failed to acquire next swap chain image"); }

			Vk.ResetFences(LogicalDevice, 1, &currentFence);

			return true;
		}

		protected unsafe void BeginFrame(VkCommandBuffer graphicsCommandBuffer, uint swapChainImageIndex) {
			Vk.ResetCommandBuffer(graphicsCommandBuffer, 0);

			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = 0, pInheritanceInfo = null, };
			if (Vk.BeginCommandBuffer(graphicsCommandBuffer, &commandBufferBeginInfo) != VkResult.Success) { throw new VulkanException("Failed to begin recording command buffer"); }

			CmdBeginPipelineBarrier(graphicsCommandBuffer, SwapChain.Images[swapChainImageIndex]);
			CmdBeginRendering(graphicsCommandBuffer, SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], Window.ClearColor.ToVkClearColorValue());
		}

		/*
		   Wait for the previous frame to finish
		   Acquire an image from the swap chain
		   Record a command buffer which draws the scene onto that image
		   Submit the recorded command buffer
		   Present the swap chain image
		 */
		protected override void DrawFrame(float delta) {
			if (!CanRender) { return; }

			if (AcquireNextImage(out uint swapChainImageIndex)) {
				BeginFrame(CurrentGraphicsCommandBuffer, swapChainImageIndex);
				DrawFrame(CurrentGraphicsCommandBuffer, delta);
				UpdateUniformBuffer(delta);
				EndFrame(CurrentGraphicsCommandBuffer, swapChainImageIndex);
				PresentFrame(swapChainImageIndex);
			}
		}

		protected abstract void DrawFrame(VkCommandBuffer graphicsCommandBuffer, float delta);

		protected void EndFrame(VkCommandBuffer graphicsCommandBuffer, uint swapChainImageIndex) {
			Vk.CmdEndRendering(graphicsCommandBuffer);
			CmdEndPipelineBarrier(graphicsCommandBuffer, SwapChain.Images[swapChainImageIndex]);

			if (Vk.EndCommandBuffer(graphicsCommandBuffer) != VkResult.Success) { throw new VulkanException("Failed to end recording command buffer"); }

			VkH.SubmitCommandBufferQueue(LogicalGpu.GraphicsQueue, graphicsCommandBuffer, CurrentImageAvailableSemaphore, RenderFinishedSemaphores[swapChainImageIndex], CurrentInFlightFence);
		}

		protected unsafe void PresentFrame(uint swapChainImageIndex) {
			VkSwapchainKHR swapChain = SwapChain.VkSwapChain;
			VkSemaphore renderFinishedSemaphore = RenderFinishedSemaphores[swapChainImageIndex];
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

			foreach (VkSemaphore renderFinishedSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(LogicalDevice, renderFinishedSemaphore, null); }

			foreach (FrameData frame in Frames) { frame.Destroy(); }
		}

		protected static unsafe void CmdBeginPipelineBarrier(VkCommandBuffer graphicsCommandBuffer, VkImage image) {
			VkImageMemoryBarrier2 imageMemoryBarrier2 = new() {
					dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
					dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
					oldLayout = VkImageLayout.ImageLayoutUndefined,
					newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					image = image,
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			VkDependencyInfo dependencyInfo = new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, };
			Vk.CmdPipelineBarrier2(graphicsCommandBuffer, &dependencyInfo);
		}

		protected static unsafe void CmdEndPipelineBarrier(VkCommandBuffer graphicsCommandBuffer, VkImage image) {
			VkImageMemoryBarrier2 imageMemoryBarrier2 = new() {
					srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
					srcStageMask = VkPipelineStageFlagBits2.PipelineStage2BottomOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
					oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
					image = image,
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			VkDependencyInfo dependencyInfo = new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, };
			Vk.CmdPipelineBarrier2(graphicsCommandBuffer, &dependencyInfo);
		}

		protected static unsafe void CmdBeginRendering(VkCommandBuffer graphicsCommandBuffer, VkExtent2D extent, VkImageView imageView, VkClearColorValue clearColor) {
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
			Vk.CmdBeginRendering(graphicsCommandBuffer, &renderingInfo);
		}

		protected class FrameData {
			public VkCommandBuffer GraphicsCommandBuffer { get; }
			public VkSemaphore ImageAvailableSemaphore { get; }
			public VkFence InFlightFence { get; }

			private readonly VkDevice logicalDevice;

			public FrameData(VkDevice logicalDevice, VkCommandBuffer graphicsCommandBuffer, VkSemaphore imageAvailableSemaphore, VkFence inFlightFence) {
				GraphicsCommandBuffer = graphicsCommandBuffer;
				ImageAvailableSemaphore = imageAvailableSemaphore;
				InFlightFence = inFlightFence;
				this.logicalDevice = logicalDevice;
			}

			public unsafe void Destroy() {
				Vk.DestroySemaphore(logicalDevice, ImageAvailableSemaphore, null);
				Vk.DestroyFence(logicalDevice, InFlightFence, null);
			}
		}
	}
}