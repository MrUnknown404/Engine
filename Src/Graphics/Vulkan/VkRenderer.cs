using Engine3.Exceptions;
using Engine3.Utils.Extensions;
using OpenTK.Graphics.Vulkan;

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

		protected VkRenderer(VkWindow window, byte maxFramesInFlight) {
			Window = window;
			MaxFramesInFlight = maxFramesInFlight;
			GraphicsCommandPool = VkH.CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, PhysicalGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = VkH.CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, PhysicalGpu.QueueFamilyIndices.TransferFamily);

			VkCommandBuffer[] graphicsCommandBuffers = VkH.CreateCommandBuffers(LogicalDevice, GraphicsCommandPool, MaxFramesInFlight);
			VkSemaphore[] imageAvailableSemaphores = VkH.CreateSemaphores(LogicalDevice, MaxFramesInFlight);
			VkSemaphore[] renderFinishedSemaphores = VkH.CreateSemaphores(LogicalDevice, MaxFramesInFlight);
			VkFence[] inFlightFences = VkH.CreateFences(LogicalDevice, MaxFramesInFlight);

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(LogicalDevice, graphicsCommandBuffers[i], imageAvailableSemaphores[i], renderFinishedSemaphores[i], inFlightFences[i]); }
		}

		protected virtual void UpdateUniformBuffer(float delta) { }

		protected unsafe bool AcquireNextImage(out uint swapChainImageIndex) {
			VkFence currentFence = CurrentInFlightFence;

			// not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			// vulkan-tutorial.com waits for all
			// vkguide.dev waits for current
			Vk.WaitForFences(LogicalDevice, 1, &currentFence, (int)Vk.True, ulong.MaxValue);

			uint tempSwapChainImageIndex;
			VkResult vkResult = Vk.AcquireNextImageKHR(LogicalDevice, SwapChain.VkSwapChain, ulong.MaxValue, CurrentImageAvailableSemaphore, VkFence.Zero, &tempSwapChainImageIndex); // TODO 2
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

			CmdBeginPipelineBarrier(graphicsCommandBuffer, SwapChain.Images, swapChainImageIndex);
			CmdBeginRendering(graphicsCommandBuffer, SwapChain.Extent, SwapChain.ImageViews, swapChainImageIndex, Window.ClearColor.ToVkClearColorValue());
		}

		protected void EndFrame(VkCommandBuffer graphicsCommandBuffer, uint swapChainImageIndex) {
			Vk.CmdEndRendering(graphicsCommandBuffer);
			CmdEndPipelineBarrier(graphicsCommandBuffer, SwapChain.Images, swapChainImageIndex);

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

		protected static unsafe void CmdBeginPipelineBarrier(VkCommandBuffer graphicsCommandBuffer, VkImage[] images, uint swapChainImageIndex) {
			VkImageMemoryBarrier imageMemoryBarrier = new() {
					dstAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutUndefined,
					newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					image = images[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(graphicsCommandBuffer, VkPipelineStageFlagBits.PipelineStageTopOfPipeBit, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, 0, 0, null, 0, null, 1, &imageMemoryBarrier); // TODO 2
		}

		protected static unsafe void CmdEndPipelineBarrier(VkCommandBuffer graphicsCommandBuffer, VkImage[] images, uint swapChainImageIndex) {
			VkImageMemoryBarrier imageMemoryBarrier = new() {
					srcAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
					image = images[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(graphicsCommandBuffer, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, VkPipelineStageFlagBits.PipelineStageBottomOfPipeBit, 0, 0, null, 0, null, 1, &imageMemoryBarrier); // TODO 2
		}

		protected static unsafe void CmdBeginRendering(VkCommandBuffer graphicsCommandBuffer, VkExtent2D extent, VkImageView[] imageViews, uint swapChainImageIndex, VkClearColorValue clearColor) {
			VkRenderingAttachmentInfo vkRenderingAttachmentInfo = new() {
					imageView = imageViews[swapChainImageIndex],
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

			public unsafe void Destroy() {
				Vk.DestroySemaphore(logicalDevice, ImageAvailableSemaphore, null);
				Vk.DestroySemaphore(logicalDevice, RenderFinishedSemaphore, null);
				Vk.DestroyFence(logicalDevice, InFlightFence, null);
			}
		}
	}
}