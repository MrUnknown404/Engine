using Engine4.Client.Graphics._Test;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Rendering;
using Engine4.IO;
using Engine4.Utility.Exceptions;
using Engine4.Utility.Math;
using NLog;
using OpenTK.Graphics.Vulkan;
using Semaphore = Engine4.Client.Graphics.Vulkan.Objects.Semaphore;

namespace Engine4.Client.Graphics.Vulkan;

// TODO add way of rendering to a texture or convert to ascii then display to console
public sealed unsafe class VulkanRenderer {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public Color4 ClearColor { get; }
	public ulong FrameCount { get; private set; }
	public bool IsFrameBufferSizeDirty { get; protected set; } // TODO move? set on resize

	private readonly VulkanResourceManager resourceManager;
	private readonly SurfaceReadyPhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;
	internal readonly Window Window;
	private readonly Surface surface;
	internal readonly SwapChain SwapChain;

	private readonly List<RenderPass> renderPasses; // TODO make sure this supports adding/removing at runtime
	private readonly RenderGraph3 renderGraph;

	private readonly GraphicsCommandPool graphicsCommandPool;
	private readonly TransferCommandPool transferCommandPool;

	// frames in flight
	private readonly FrameInFlight[] framesInFlight;
	private readonly byte maxFramesInFlight;
	private byte currentFrameInFlight;

	// swap chain
	private readonly Semaphore[] renderFinishedSemaphores;
	private uint swapChainImageIndex;

	// TODO easy depth image
	private DepthImage? depthImage; // TODO merge into render graph?

	internal VulkanRenderer(string debugName, VulkanManager vulkanManager, Window window, Color4 clearColor, params RenderPass[] renderPasses) {
		Window = window;
		ClearColor = clearColor;

		// TODO log
		surface = new(vulkanManager.VulkanInstance, window);
		SurfaceReadyPhysicalGpu[] capableGpus = vulkanManager.GetCapableGpus(surface);
		physicalGpu = vulkanManager.SelectGpu(capableGpus) ?? throw new Engine4Exception("Failed to select gpu");
		logicalGpu = new(physicalGpu, vulkanManager);
		SwapChain = new(window, physicalGpu, logicalGpu, surface, vulkanManager.PresentMode);
		renderFinishedSemaphores = logicalGpu.ResourceManager.CreateSemaphores("Render Finished Semaphore", 0, (uint)SwapChain.Images.Length);

		resourceManager = logicalGpu.ResourceManager;
		this.renderPasses = new(renderPasses);
		maxFramesInFlight = vulkanManager.MaxFramesInFlight;

		renderGraph = new(this, resourceManager);

		// TODO logging
		graphicsCommandPool = resourceManager.CreateGraphicsCommandPool($"{debugName} Graphics Command Pool", VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit);
		transferCommandPool = resourceManager.CreateTransferCommandPool($"{debugName} Transfer Command Pool", VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit);

		GraphicsCommandBuffer[] graphicsCommandBuffers = graphicsCommandPool.CreateCommandBuffers(maxFramesInFlight, VkCommandBufferLevel.CommandBufferLevelPrimary);
		Semaphore[] imageAvailableSemaphores = resourceManager.CreateSemaphores($"{debugName} Image Available Semaphore", 0, maxFramesInFlight);
		Fence[] inFlightFences = resourceManager.CreateFences($"{debugName} In Flight Fence", maxFramesInFlight, VkFenceCreateFlagBits.FenceCreateSignaledBit);

		framesInFlight = new FrameInFlight[maxFramesInFlight];
		for (int i = 0; i < maxFramesInFlight; i++) { framesInFlight[i] = new(graphicsCommandBuffers[i], imageAvailableSemaphores[i], inFlightFences[i]); }
	}

	internal void InternalRender(float delta) {
		if (Render(delta)) { FrameCount++; }
	}

	private bool Render(float delta) {
		FrameInFlight frame = framesInFlight[currentFrameInFlight];
		Fence inFlightFence = frame.InFlightFence;

		// TODO not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
		//  vulkan-tutorial.com waits for all
		//  vkguide.dev waits for current
		inFlightFence.Wait(true, uint.MaxValue);

		if (TryBeginFrame(frame)) {
			inFlightFence.Reset();

			// update buffers/etc
			UpdateBuffers(delta);
			SyncResources(); // TODO how do i handle this?

			// draw
			GraphicsCommandBuffer graphicsCommandBuffer = frame.GraphicsCommandBuffer;
			BeginFrame(graphicsCommandBuffer);
			DrawFrame(graphicsCommandBuffer);
			EndFrame(graphicsCommandBuffer);
			SubmitQueue(frame);

			PresentFrame();

			currentFrameInFlight = (byte)((currentFrameInFlight + 1) % maxFramesInFlight);
			return true;
		}

		return false;
	}

	private void UpdateBuffers(float delta) { } // TODO
	private void SyncResources() { } // TODO

	private bool TryBeginFrame(FrameInFlight frame) {
		VkResult result = SwapChain.AcquireNextImage(frame.ImageAvailableSemaphore, out swapChainImageIndex);
		if (result == VkResult.ErrorOutOfDateKhr) {
			InvalidateSwapChain();
			return false;
		} else if (result != VkResult.SuboptimalKhr) { VkH.CheckSuccess(result, "Failed to acquire next swap chain image"); }

		return true;
	}

	private void BeginFrame(GraphicsCommandBuffer graphicsCommandBuffer) {
		graphicsCommandBuffer.ResetCommandBuffer();
		VkH.CheckSuccess(graphicsCommandBuffer.BeginCommandBuffer(0), "Failed to begin command buffer");

		VkImageMemoryBarrier2 imageMemoryBarrier = new() {
				dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
				dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
				oldLayout = VkImageLayout.ImageLayoutUndefined,
				newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
				image = GetSwapChainImage(),
				subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
		};

		graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, });

		// graphicsCommandBuffer.CmdBeginRendering(swapChain.Extent, swapChain.ImageViews[swapChainImageIndex], ClearColor, depthImage?.Image, new(1, 0)); // TODO use depth image
	}

	private void DrawFrame(GraphicsCommandBuffer commandBuffer) {
		// RecordCommandBuffer(graphicsCommandBuffer); // TODO draw
		// renderGraph.Render(graphicsCommandBuffer, LogicalGpu.GraphicsQueue, LogicalGpu.TransferQueue);

		renderGraph.Render(commandBuffer);

		// commandBuffer.CmdBeginRendering();
		// // draw
		// commandBuffer.CmdEndRendering();
	}

	private void EndFrame(GraphicsCommandBuffer graphicsCommandBuffer) {
		// graphicsCommandBuffer.CmdEndRendering();

		VkImageMemoryBarrier2 imageMemoryBarrier = new() {
				srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
				srcStageMask = VkPipelineStageFlagBits2.PipelineStage2BottomOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
				oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
				newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
				image = GetSwapChainImage(),
				subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
		};

		graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, });

		VkH.CheckSuccess(graphicsCommandBuffer.EndCommandBuffer(), "Failed to end command buffer");
	}

	private void SubmitQueue(FrameInFlight frame) {
		VkPipelineStageFlagBits* waitStages = stackalloc VkPipelineStageFlagBits[1] { VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, };
		VkSemaphore signalSemaphore = renderFinishedSemaphores[swapChainImageIndex].VkSemaphore; // lots of copying. can i fix that?
		VkSemaphore waitSemaphore = frame.ImageAvailableSemaphore.VkSemaphore;
		VkCommandBuffer commandBuffer = frame.GraphicsCommandBuffer.VkCommandBuffer;

		VkSubmitInfo submitInfo = new() {
				waitSemaphoreCount = 1,
				pWaitSemaphores = &waitSemaphore,
				pWaitDstStageMask = waitStages,
				commandBufferCount = 1,
				pCommandBuffers = &commandBuffer,
				signalSemaphoreCount = 1,
				pSignalSemaphores = &signalSemaphore,
		};

		Vk.QueueSubmit(logicalGpu.GraphicsQueue, 1, &submitInfo, frame.InFlightFence.VkFence);
	}

	private void PresentFrame() {
		VkSwapchainKHR swapChain = SwapChain.VkSwapChain;
		VkSemaphore waitSemaphore = renderFinishedSemaphores[swapChainImageIndex].VkSemaphore;

		VkResult result;
		fixed (uint* swapChainImageIndexPtr = &swapChainImageIndex) {
			VkPresentInfoKHR presentInfo = new() { waitSemaphoreCount = 1, pWaitSemaphores = &waitSemaphore, swapchainCount = 1, pSwapchains = &swapChain, pImageIndices = swapChainImageIndexPtr, };
			result = Vk.QueuePresentKHR(logicalGpu.PresentQueue, &presentInfo);
		}

		if (result is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr || IsFrameBufferSizeDirty) {
			IsFrameBufferSizeDirty = false;
			InvalidateSwapChain();
		} else { VkH.CheckSuccess(result, "Failed to present queue"); }
	}

	private void InvalidateSwapChain() {
		Logger.Trace("Swapchain is invalid. Recreating...");

		SwapChain.Recreate();
		// DepthImage?.Recreate(SwapChain.Extent);
	}

	internal VkFormat GetDepthFormat() => physicalGpu.FindDepthFormat();
	internal VkImage GetSwapChainImage() => SwapChain.Images[swapChainImageIndex];
	internal VkImageView GetSwapChainImageView() => SwapChain.ImageViews[swapChainImageIndex];

	internal void Cleanup() {
		Vk.DeviceWaitIdle(logicalGpu.VkLogicalDevice);

		SwapChain.Cleanup();
		surface.Cleanup();

		logicalGpu.Cleanup();
	}

	private class FrameInFlight {
		public GraphicsCommandBuffer GraphicsCommandBuffer { get; }
		public Semaphore ImageAvailableSemaphore { get; }
		public Fence InFlightFence { get; }

		public FrameInFlight(GraphicsCommandBuffer graphicsCommandBuffer, Semaphore imageAvailableSemaphore, Fence inFlightFence) {
			GraphicsCommandBuffer = graphicsCommandBuffer;
			ImageAvailableSemaphore = imageAvailableSemaphore;
			InFlightFence = inFlightFence;
		}
	}
}