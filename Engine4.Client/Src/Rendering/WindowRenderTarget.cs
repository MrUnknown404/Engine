using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.IO;
using Engine4.Utility.Exceptions;
using Engine4.Utility.Math;
using NLog;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;
using Semaphore = Engine4.Client.Graphics.Vulkan.Objects.Semaphore;

namespace Engine4.Client.Rendering;

public sealed unsafe class WindowRenderTarget : RenderTarget {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public override SurfaceReadyPhysicalGpu PhysicalGpu { get; }
	public override LogicalGpu LogicalGpu { get; }

	private readonly Window window;
	private readonly Surface surface;
	private readonly SwapChain swapChain;

	// this is messy
	private readonly Semaphore[] renderFinishedSemaphores;
	private uint swapChainImageIndex;

	internal WindowRenderTarget(Window window, VulkanManager vulkanManager, VulkanInstance vulkanInstance, Color3 clearColor) : base(clearColor) {
		this.window = window;

		// TODO logging
		surface = new(vulkanInstance, window);
		SurfaceReadyPhysicalGpu[] capableGpus = vulkanManager.GetCapableGpus(surface);
		PhysicalGpu = vulkanManager.SelectGpu(capableGpus) ?? throw new Engine4Exception("Failed to select gpu");
		LogicalGpu = new(PhysicalGpu, vulkanManager);
		swapChain = new(window, PhysicalGpu, LogicalGpu, surface, vulkanManager.PresentMode);
		renderFinishedSemaphores = LogicalGpu.ResourceManager.CreateSemaphores("Render Finished Semaphore", 0, (uint)swapChain.Images.Length);
	}

	protected internal override bool TryBeginFrame(VulkanRenderer.FrameInFlight frame) {
		VkResult result = swapChain.AcquireNextImage(frame.ImageAvailableSemaphore, out swapChainImageIndex);
		if (result == VkResult.ErrorOutOfDateKhr) {
			InvalidateSwapChain();
			return false;
		} else if (result != VkResult.SuboptimalKhr) { VkH.CheckSuccess(result, "Failed to acquire next swap chain image"); }

		return true;
	}

	protected internal override void CmdBeginRendering(GraphicsCommandBuffer graphicsCommandBuffer, DepthImage? depthImage) {
		VkImageMemoryBarrier2 imageMemoryBarrier2 = GetBeginPipelineBarrierImageMemoryBarrier();
		graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

		graphicsCommandBuffer.CmdBeginRendering(swapChain.Extent, swapChain.ImageViews[swapChainImageIndex], depthImage == null ? null : null, ClearColor, new(1, 0)); // TODO use depth image
	}

	protected internal override void CmdEndRendering(GraphicsCommandBuffer graphicsCommandBuffer) {
		graphicsCommandBuffer.CmdEndRendering();

		VkImageMemoryBarrier2 imageMemoryBarrier2 = GetEndPipelineBarrierImageMemoryBarrier();
		graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });
	}

	protected internal override void PresentFrame(VulkanRenderer.FrameInFlight frame) {
		VkSwapchainKHR swapChain = this.swapChain.VkSwapChain;
		uint swapChainImageIndex = this.swapChainImageIndex;
		VkSemaphore renderFinishedSemaphore = renderFinishedSemaphores[swapChainImageIndex].VkSemaphore;

		VkPresentInfoKHR presentInfo = new() { waitSemaphoreCount = 1, pWaitSemaphores = &renderFinishedSemaphore, swapchainCount = 1, pSwapchains = &swapChain, pImageIndices = &swapChainImageIndex, };
		VkResult result = Vk.QueuePresentKHR(LogicalGpu.PresentQueue, &presentInfo);

		if (result is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr || IsFrameBufferDirty) {
			IsFrameBufferDirty = false;
			InvalidateSwapChain();
		} else { VkH.CheckSuccess(result, "Failed to present queue"); }
	}

	protected internal override Semaphore GetSignalSemaphore() => renderFinishedSemaphores[swapChainImageIndex];

	public override Vec2<ushort> GetFrameBufferSize() => window.GetFrameBufferSize();

	private void InvalidateSwapChain() {
		Logger.Trace("Swapchain is invalid. Recreating...");

		swapChain.Recreate();
		// DepthImage?.Recreate(SwapChain.Extent);
	}

	protected internal override void Cleanup() {
		// all resources that use logical gpu should be cleaned here
		Vk.DeviceWaitIdle(LogicalGpu.VkLogicalDevice);

		swapChain.Cleanup();
		surface.Cleanup();

		LogicalGpu.Cleanup();
	}

	private VkImageMemoryBarrier2 GetBeginPipelineBarrierImageMemoryBarrier() => // TODO rename
			new() {
					dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
					dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
					oldLayout = VkImageLayout.ImageLayoutUndefined,
					newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					image = swapChain.Images[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

	private VkImageMemoryBarrier2 GetEndPipelineBarrierImageMemoryBarrier() => // TODO rename
			new() {
					srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
					srcStageMask = VkPipelineStageFlagBits2.PipelineStage2BottomOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
					oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
					image = swapChain.Images[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};
}