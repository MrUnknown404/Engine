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
	public bool IsFrameBufferSizeDirty { get; private set; } // TODO move? set on resize

	public VulkanResourceManager ResourceManager { get; }
	private readonly SurfaceReadyPhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;
	private readonly Window window;
	private readonly Surface surface;
	private readonly SwapChain swapChain;

	// TODO create render graph system
	[Obsolete]
	private readonly List<RenderPass> renderPasses = new();
	private DepthImage? depthImage;

	private readonly GraphicsCommandPool graphicsCommandPool;
	private readonly TransferCommandPool transferCommandPool;

	// frames in flight
	private readonly FrameInFlight[] framesInFlight;
	private readonly byte maxFramesInFlight;
	private byte currentFrameInFlight;

	// swap chain
	private readonly Semaphore[] renderFinishedSemaphores;
	private uint swapChainImageIndex;

	internal VulkanRenderer(string debugName, VulkanManager vulkanManager, Window window, Color4 clearColor) {
		this.window = window;
		ClearColor = clearColor;

		// TODO log
		surface = new(vulkanManager.VulkanInstance, window);
		SurfaceReadyPhysicalGpu[] capableGpus = vulkanManager.GetCapableGpus(surface);
		physicalGpu = vulkanManager.SelectGpu(capableGpus) ?? throw new Engine4Exception("Failed to select gpu");
		logicalGpu = new(physicalGpu, vulkanManager);
		swapChain = new(window, physicalGpu, logicalGpu, surface, vulkanManager.PresentMode);
		renderFinishedSemaphores = logicalGpu.ResourceManager.CreateSemaphores("Render Finished Semaphore", 0, (uint)swapChain.Images.Length);

		ResourceManager = logicalGpu.ResourceManager;
		maxFramesInFlight = vulkanManager.MaxFramesInFlight;

		// TODO logging
		graphicsCommandPool = ResourceManager.CreateGraphicsCommandPool($"{debugName} Graphics Command Pool", VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit);
		transferCommandPool = ResourceManager.CreateTransferCommandPool($"{debugName} Transfer Command Pool", VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit);

		GraphicsCommandBuffer[] graphicsCommandBuffers = graphicsCommandPool.CreateCommandBuffers(maxFramesInFlight, VkCommandBufferLevel.CommandBufferLevelPrimary);
		Semaphore[] imageAvailableSemaphores = ResourceManager.CreateSemaphores($"{debugName} Image Available Semaphore", 0, maxFramesInFlight);
		Fence[] inFlightFences = ResourceManager.CreateFences($"{debugName} In Flight Fence", maxFramesInFlight, VkFenceCreateFlagBits.FenceCreateSignaledBit);

		framesInFlight = new FrameInFlight[maxFramesInFlight];
		for (int i = 0; i < maxFramesInFlight; i++) { framesInFlight[i] = new(graphicsCommandBuffers[i], imageAvailableSemaphores[i], inFlightFences[i]); }
	}

	public void AddRenderPass(RenderPass renderPass) => renderPasses.Add(renderPass);
	public void RemoveRenderPass(RenderPass renderPass) => throw new NotImplementedException(); // TODO

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
		VkResult result = swapChain.AcquireNextImage(frame.ImageAvailableSemaphore, out swapChainImageIndex);
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
				image = swapChain.Images[swapChainImageIndex],
				subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
		};

		graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier, });

		graphicsCommandBuffer.CmdBeginRendering(swapChain.Extent, GetSwapChainImageView(), ClearColor, depthImage?.Image, new(1, 0)); // TODO use depth image
	}

	private void DrawFrame(GraphicsCommandBuffer commandBuffer) {
		// RecordCommandBuffer(graphicsCommandBuffer); // TODO draw
		// renderGraph.Render(graphicsCommandBuffer, LogicalGpu.GraphicsQueue, LogicalGpu.TransferQueue);

		foreach (RenderPass renderPass in renderPasses) { renderPass.RecordCommandBuffer(commandBuffer); }

		// commandBuffer.CmdBeginRendering();
		// // draw
		// commandBuffer.CmdEndRendering();
	}

	private void EndFrame(GraphicsCommandBuffer graphicsCommandBuffer) {
		graphicsCommandBuffer.CmdEndRendering();

		VkImageMemoryBarrier2 imageMemoryBarrier = new() {
				srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
				srcStageMask = VkPipelineStageFlagBits2.PipelineStage2BottomOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
				oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
				newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
				image = swapChain.Images[swapChainImageIndex],
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
		VkSwapchainKHR swapChain = this.swapChain.VkSwapChain;
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

		swapChain.Recreate();
		// DepthImage?.Recreate(SwapChain.Extent);
	}

	public VkFormat GetDepthFormat() => physicalGpu.DepthFormat;

	public VkExtent2D GetSwapChainExtent() => swapChain.Extent;
	public VkFormat GetSwapChainFormat() => swapChain.ImageFormat;
	public VkImageView GetSwapChainImageView() => swapChain.ImageViews[swapChainImageIndex];

	internal void Cleanup() {
		Vk.DeviceWaitIdle(logicalGpu.VkLogicalDevice);

		swapChain.Cleanup();
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