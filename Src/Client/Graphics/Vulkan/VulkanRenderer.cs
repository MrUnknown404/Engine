using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility.Extensions;
using ImGuiNET;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public abstract unsafe class VulkanRenderer : Renderer<VulkanWindow, VulkanGraphicsBackend, VulkanImGuiBackend> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected SwapChain SwapChain { get; }

		protected GraphicsCommandPool GraphicsCommandPool { get; }
		protected TransferCommandPool TransferCommandPool { get; }

		protected FrameData[] Frames { get; }
		protected VkSemaphore[] RenderFinishedSemaphores { get; }

		protected virtual DepthImage? DepthImage => null;

		protected byte FrameIndex { get; private set; }

		protected SurfaceCapablePhysicalGpu PhysicalGpu => Window.SelectedGpu;
		protected LogicalGpu LogicalGpu => Window.LogicalGpu;
		protected byte MaxFramesInFlight => GraphicsBackend.MaxFramesInFlight;

		protected VulkanRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window) : base(graphicsBackend, window) {
			SwapChain = new(window, window.SelectedGpu.PhysicalDevice, window.LogicalGpu.LogicalDevice, window.SelectedGpu.QueueFamilyIndices, window.Surface, graphicsBackend.PresentMode);
			Logger.Debug("Created swap chain");

			GraphicsCommandPool = LogicalGpu.CreateGraphicsCommandPool(VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, Window.SelectedGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = LogicalGpu.CreateTransferCommandPool(VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, Window.SelectedGpu.QueueFamilyIndices.TransferFamily);
			RenderFinishedSemaphores = LogicalGpu.CreateSemaphores((uint)SwapChain.Images.Length);

			GraphicsCommandBuffer[] graphicsCommandBuffers = GraphicsCommandPool.CreateCommandBuffers(MaxFramesInFlight);
			VkSemaphore[] imageAvailableSemaphores = LogicalGpu.CreateSemaphores(MaxFramesInFlight);
			VkFence[] inFlightFences = LogicalGpu.CreateFences(MaxFramesInFlight);

			VkDevice logicalDevice = LogicalGpu.LogicalDevice;

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(logicalDevice, graphicsCommandBuffers[i], imageAvailableSemaphores[i], inFlightFences[i]); }
		}

		protected internal override void Setup() => ImGuiBackend?.Setup(TransferCommandPool, SwapChain.ImageFormat);

		/// <summary>
		/// Wait for the previous frame to finish
		/// Acquire an image from the swap chain
		/// Record a command buffer which draws the scene onto that image
		/// Submit the recorded command buffer
		/// Present the swap chain image
		/// </summary>
		protected internal override void Render(float delta) {
			LogicalGpu.TryCleanupResources(); // TODO don't destroy every frame?

			FrameData frameData = Frames[FrameIndex];
			if (AcquireNextImage(frameData, out uint swapChainImageIndex)) {
				// copy buffers
				CopyBuffers(delta);

				// draw
				BeginFrame(frameData, swapChainImageIndex);

				GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;
				RecordCommandBuffer(graphicsCommandBuffer);

				// update imgui buffers then draw
				if (TryImGuiNewFrame(out ImDrawDataPtr? imDrawData)) { ImGuiBackend!.RecordCommandBuffer(graphicsCommandBuffer, FrameIndex, imDrawData.Value); } // ImGuiBackend shouldn't be null if TryImGuiNewFrame returned true

				// end
				EndFrame(frameData, swapChainImageIndex);
				SubmitQueue(frameData.ImageAvailableSemaphore, [ graphicsCommandBuffer.VkCommandBuffer, ], swapChainImageIndex, frameData.InFlightFence);
				PresentFrame(swapChainImageIndex);
			}
		}

		protected virtual bool AcquireNextImage(FrameData frameData, out uint swapChainImageIndex) {
			VkDevice logicalDevice = LogicalGpu.LogicalDevice;
			VkFence inFlightFence = frameData.InFlightFence;

			// not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			// vulkan-tutorial.com waits for all
			// vkguide.dev waits for current
			Vk.WaitForFences(logicalDevice, 1, &inFlightFence, (int)Vk.True, ulong.MaxValue);

			VkResult result = SwapChain.AcquireNextImage(frameData.ImageAvailableSemaphore, out swapChainImageIndex);

			if (result == VkResult.ErrorOutOfDateKhr) {
				OnSwapchainInvalid();
				return false;
			} else if (result != VkResult.SuboptimalKhr) { VkH.CheckIfSuccess(result, VulkanException.Reason.AcquireNextImage); }

			Vk.ResetFences(logicalDevice, 1, &inFlightFence);

			return true;
		}

		/// <summary>
		/// Order of what vulkan methods are called here
		/// <code>
		/// vkResetCommandBuffer
		/// vkBeginCommandBuffer
		/// vkCmdPipelineBarrier (Begin)
		/// vkCmdBeginRendering
		/// </code>
		/// </summary>
		protected virtual void BeginFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.ResetCommandBuffer();

			VkH.CheckIfSuccess(graphicsCommandBuffer.BeginCommandBuffer(0), VulkanException.Reason.BeginCommandBuffer);

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetBeginPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			graphicsCommandBuffer.CmdBeginRendering(SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], DepthImage?.Image.ImageView, Window.ClearColor.ToVkClearColorValue(), new(1, 0));
		}

		protected abstract void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer);

		protected virtual void CopyBuffers(float delta) { }

		/// <summary>
		/// Order of what vulkan methods are called here
		/// <code>
		/// vkCmdEndRendering
		/// vkCmdPipelineBarrier (End)
		/// vkEndCommandBuffer
		/// </code>
		/// </summary>
		protected virtual void EndFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.CmdEndRendering();

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetEndPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			VkH.CheckIfSuccess(graphicsCommandBuffer.EndCommandBuffer(), VulkanException.Reason.EndCommandBuffer);
		}

		protected virtual void SubmitQueue(VkSemaphore waitSemaphore, VkCommandBuffer[] commandBuffers, uint swapChainImageIndex, VkFence fence) {
			VkPipelineStageFlagBits* waitStages = stackalloc VkPipelineStageFlagBits[] { VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, };
			VkSemaphore signalSemaphore = RenderFinishedSemaphores[swapChainImageIndex];

			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				VkSubmitInfo a = new() {
						waitSemaphoreCount = 1,
						pWaitSemaphores = &waitSemaphore,
						pWaitDstStageMask = waitStages,
						commandBufferCount = (uint)commandBuffers.Length,
						pCommandBuffers = commandBuffersPtr,
						signalSemaphoreCount = 1,
						pSignalSemaphores = &signalSemaphore,
				};

				Vk.QueueSubmit(LogicalGpu.GraphicsQueue, 1, &a, fence);
			}
		}

		protected virtual void PresentFrame(uint swapChainImageIndex) {
			VkSwapchainKHR swapChain = SwapChain.VkSwapChain;
			VkSemaphore renderFinishedSemaphore = RenderFinishedSemaphores[swapChainImageIndex];

			VkPresentInfoKHR presentInfo = new() { waitSemaphoreCount = 1, pWaitSemaphores = &renderFinishedSemaphore, swapchainCount = 1, pSwapchains = &swapChain, pImageIndices = &swapChainImageIndex, };
			VkResult result = Vk.QueuePresentKHR(LogicalGpu.PresentQueue, &presentInfo);

			if (result is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr || Window.WasResized) {
				Window.WasResized = false;
				OnSwapchainInvalid();
			} else { VkH.CheckIfSuccess(result, VulkanException.Reason.QueuePresent); }

			IncrementFrameIndex();
		}

		protected void IncrementFrameIndex() => FrameIndex = (byte)((FrameIndex + 1) % MaxFramesInFlight);

		protected virtual void OnSwapchainInvalid() {
			SwapChain.Recreate();
			DepthImage?.Recreate(SwapChain.Extent);
		}

		protected override void PrepareCleanup() => Vk.DeviceWaitIdle(LogicalGpu.LogicalDevice);

		protected override void Cleanup() {
			foreach (VkSemaphore renderFinishedSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(LogicalGpu.LogicalDevice, renderFinishedSemaphore, null); }
			foreach (FrameData frame in Frames) { frame.Destroy(); }

			SwapChain.Destroy();
		}

		protected static VkImageMemoryBarrier2 GetBeginPipelineBarrierImageMemoryBarrier(VkImage image) => // TODO rename
				new() {
						dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
						oldLayout = VkImageLayout.ImageLayoutUndefined,
						newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
						image = image,
						subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
				};

		protected static VkImageMemoryBarrier2 GetEndPipelineBarrierImageMemoryBarrier(VkImage image) => // TODO rename
				new() {
						srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
						srcStageMask = VkPipelineStageFlagBits2.PipelineStage2BottomOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
						oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
						newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
						image = image,
						subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
				};

		protected class FrameData {
			public GraphicsCommandBuffer GraphicsCommandBuffer { get; }
			public VkSemaphore ImageAvailableSemaphore { get; }
			public VkFence InFlightFence { get; }

			private readonly VkDevice logicalDevice;

			public FrameData(VkDevice logicalDevice, GraphicsCommandBuffer graphicsCommandBuffer, VkSemaphore imageAvailableSemaphore, VkFence inFlightFence) {
				this.logicalDevice = logicalDevice;
				GraphicsCommandBuffer = graphicsCommandBuffer;
				ImageAvailableSemaphore = imageAvailableSemaphore;
				InFlightFence = inFlightFence;
			}

			public void Destroy() {
				Vk.DestroySemaphore(logicalDevice, ImageAvailableSemaphore, null);
				Vk.DestroyFence(logicalDevice, InFlightFence, null);
			}
		}
	}
}