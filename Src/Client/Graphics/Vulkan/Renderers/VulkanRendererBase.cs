using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility.Extensions;
using ImGuiNET;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Renderers {
	public abstract unsafe class VulkanRendererBase : Renderer<VulkanWindow, VulkanImGuiBackend> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public SwapChain SwapChain { get; }

		public GraphicsCommandPool GraphicsCommandPool { get; }
		public TransferCommandPool TransferCommandPool { get; }

		protected FrameData[] Frames { get; }
		protected VkSemaphore[] RenderFinishedSemaphores { get; }

		protected virtual DepthImage? DepthImage => null; // TODO automate creating this

		protected byte FrameIndex { get; private set; }

		public SurfaceCapablePhysicalGpu PhysicalGpu => Window.SelectedGpu;
		public LogicalGpu LogicalGpu => Window.LogicalGpu;
		public byte MaxFramesInFlight { get; }

		private FrameData frameData;
		private uint swapChainImageIndex;
		private GraphicsCommandBuffer graphicsCommandBuffer;

		protected VulkanRendererBase(VulkanGraphicsBackend graphicsBackend, VulkanWindow window) : base(window) {
			MaxFramesInFlight = graphicsBackend.Settings.MaxFramesInFlight;

			SwapChain = new(window, window.SelectedGpu.PhysicalDevice, window.LogicalGpu.LogicalDevice, window.SelectedGpu.QueueFamilyIndices, window.Surface, graphicsBackend.Settings.PresentMode);
			Logger.Trace("Created swap chain");

			GraphicsCommandPool = LogicalGpu.CreateGraphicsCommandPool(VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, window.SelectedGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = LogicalGpu.CreateTransferCommandPool(VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, window.SelectedGpu.QueueFamilyIndices.TransferFamily);
			Logger.Trace("Created command pools");

			GraphicsCommandBuffer[] graphicsCommandBuffers = GraphicsCommandPool.CreateCommandBuffers(MaxFramesInFlight);
			Logger.Trace("Created command buffers");

			RenderFinishedSemaphores = LogicalGpu.CreateSemaphores((uint)SwapChain.Images.Length);
			VkSemaphore[] imageAvailableSemaphores = LogicalGpu.CreateSemaphores(MaxFramesInFlight);
			VkFence[] inFlightFences = LogicalGpu.CreateFences(MaxFramesInFlight);
			Logger.Trace("Created synchronization objects");

			VkDevice logicalDevice = LogicalGpu.LogicalDevice;

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(logicalDevice, graphicsCommandBuffers[i], imageAvailableSemaphores[i], inFlightFences[i]); }

			frameData = Frames[0];
			graphicsCommandBuffer = graphicsCommandBuffers[0];
		}

		protected internal override void Setup() => ImGuiBackend?.Setup(TransferCommandPool, SwapChain.ImageFormat);

		protected override void PrepareRender() { }
		protected override void TryCleanupResources() => LogicalGpu.TryCleanupResources();

		protected override bool TryNextFrame() {
			frameData = Frames[FrameIndex];

			VkDevice logicalDevice = LogicalGpu.LogicalDevice;
			VkFence inFlightFence = frameData.InFlightFence;

			// TODO not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			//  vulkan-tutorial.com waits for all
			//  vkguide.dev waits for current
			Vk.WaitForFences(logicalDevice, 1, &inFlightFence, VkH.True, ulong.MaxValue);

			VkResult result = SwapChain.AcquireNextImage(frameData.ImageAvailableSemaphore, out swapChainImageIndex);

			if (result == VkResult.ErrorOutOfDateKhr) {
				OnSwapchainInvalid();
				return false;
			} else if (result != VkResult.SuboptimalKhr) { VkH.CheckIfSuccess(result, VulkanException.Reason.AcquireNextImage); }

			Vk.ResetFences(logicalDevice, 1, &inFlightFence);

			return true;
		}

		protected override void BeginFrame() {
			graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.ResetCommandBuffer();

			VkH.CheckIfSuccess(graphicsCommandBuffer.BeginCommandBuffer(0), VulkanException.Reason.BeginCommandBuffer);

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetBeginPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			graphicsCommandBuffer.CmdBeginRendering(SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], DepthImage?.Image.ImageView, Window.ClearColor.ToVkClearColorValue(), new(1, 0));
		}

		protected override void DrawFrame() => RecordCommandBuffer(graphicsCommandBuffer);

		protected override void DrawImGuiFrame(ImDrawDataPtr imDrawData) => ImGuiBackend!.RecordCommandBuffer(graphicsCommandBuffer, FrameIndex, imDrawData); // shouldn't be null if this is called

		protected override void EndFrame() {
			graphicsCommandBuffer.CmdEndRendering();

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetEndPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			VkH.CheckIfSuccess(graphicsCommandBuffer.EndCommandBuffer(), VulkanException.Reason.EndCommandBuffer);

			SubmitQueue(frameData.ImageAvailableSemaphore, [ graphicsCommandBuffer.VkCommandBuffer, ], swapChainImageIndex, frameData.InFlightFence); //

			PresentFrame(swapChainImageIndex);
		}

		protected abstract void RecordCommandBuffer(GraphicsCommandBuffer commandBuffer);

		protected virtual void SubmitQueue(VkSemaphore waitSemaphore, VkCommandBuffer[] commandBuffers, uint swapChainImageIndex, VkFence fence) {
			VkPipelineStageFlagBits* waitStages = stackalloc VkPipelineStageFlagBits[] { VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, };
			VkSemaphore signalSemaphore = RenderFinishedSemaphores[swapChainImageIndex];

			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				VkSubmitInfo submitInfo = new() {
						waitSemaphoreCount = 1,
						pWaitSemaphores = &waitSemaphore,
						pWaitDstStageMask = waitStages,
						commandBufferCount = (uint)commandBuffers.Length,
						pCommandBuffers = commandBuffersPtr,
						signalSemaphoreCount = 1,
						pSignalSemaphores = &signalSemaphore,
				};

				Vk.QueueSubmit(LogicalGpu.GraphicsQueue, 1, &submitInfo, fence);
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