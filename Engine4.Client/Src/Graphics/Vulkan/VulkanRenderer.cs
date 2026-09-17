using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Rendering;
using Engine4.IO;
using NLog;
using OpenTK.Graphics.Vulkan;
using Semaphore = Engine4.Client.Graphics.Vulkan.Objects.Semaphore;

namespace Engine4.Client.Graphics.Vulkan;

public sealed unsafe class VulkanRenderer {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public ulong FrameCount { get; private set; }

	private readonly VulkanResourceManager resourceManager;
	private readonly List<RenderPass> renderPasses; // TODO make sure this supports adding/removing at runtime
	internal RenderTarget RenderTarget { get; }

	private readonly GraphicsCommandPool graphicsCommandPool;
	private readonly TransferCommandPool transferCommandPool;

	private readonly FrameInFlight[] framesInFlight;
	private readonly byte maxFramesInFlight;
	private byte currentFrameInFlight;

	// TODO easy depth image
	private DepthImage? depthImage;

	internal VulkanRenderer(string debugName, VulkanManager vulkanManager, RenderTarget renderTarget, params RenderPass[] renderPasses) {
		if (renderTarget.InUse) { throw new Exception(); } // TODO exception

		resourceManager = renderTarget.LogicalGpu.ResourceManager;
		RenderTarget = renderTarget;
		this.renderPasses = new(renderPasses);
		maxFramesInFlight = vulkanManager.MaxFramesInFlight;

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

		if (RenderTarget.TryBeginFrame(frame)) {
			inFlightFence.Reset();

			// update buffers/etc
			UpdateBuffers(delta);
			SyncResources(); // TODO how do i handle this?

			// draw
			BeginFrame(frame);
			DrawFrame(frame);
			EndFrame(frame);

			RenderTarget.PresentFrame(frame);

			currentFrameInFlight = (byte)((currentFrameInFlight + 1) % maxFramesInFlight);
			return true;
		}

		return false;
	}

	private void UpdateBuffers(float delta) { } // TODO
	private void SyncResources() { } // TODO

	private void BeginFrame(FrameInFlight frame) {
		GraphicsCommandBuffer graphicsCommandBuffer = frame.GraphicsCommandBuffer;

		graphicsCommandBuffer.ResetCommandBuffer();
		VkH.CheckSuccess(graphicsCommandBuffer.BeginCommandBuffer(0), "Failed to begin command buffer");
		RenderTarget.CmdBeginRendering(graphicsCommandBuffer, depthImage);
	}

	private void DrawFrame(FrameInFlight frame) {
		GraphicsCommandBuffer graphicsCommandBuffer = frame.GraphicsCommandBuffer;
		// RecordCommandBuffer(graphicsCommandBuffer); // TODO draw
	}

	private void EndFrame(FrameInFlight frame) {
		GraphicsCommandBuffer graphicsCommandBuffer = frame.GraphicsCommandBuffer;

		RenderTarget.CmdEndRendering(graphicsCommandBuffer);
		VkH.CheckSuccess(graphicsCommandBuffer.EndCommandBuffer(), "Failed to end command buffer");

		// submit queue
		VkPipelineStageFlagBits* waitStages = stackalloc VkPipelineStageFlagBits[] { VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, };
		VkSemaphore signalSemaphore = RenderTarget.GetSignalSemaphore().VkSemaphore;
		VkSemaphore imageAvailableSemaphore = frame.ImageAvailableSemaphore.VkSemaphore;
		VkCommandBuffer commandBuffer = graphicsCommandBuffer.VkCommandBuffer;

		VkSubmitInfo submitInfo = new() {
				waitSemaphoreCount = 1,
				pWaitSemaphores = &imageAvailableSemaphore,
				pWaitDstStageMask = waitStages,
				commandBufferCount = 1,
				pCommandBuffers = &commandBuffer,
				signalSemaphoreCount = 1,
				pSignalSemaphores = &signalSemaphore,
		};

		Vk.QueueSubmit(RenderTarget.LogicalGpu.GraphicsQueue, 1, &submitInfo, frame.InFlightFence.VkFence);
	}

	public class FrameInFlight {
		public GraphicsCommandBuffer GraphicsCommandBuffer { get; }
		public Semaphore ImageAvailableSemaphore { get; }
		public Fence InFlightFence { get; }

		internal FrameInFlight(GraphicsCommandBuffer graphicsCommandBuffer, Semaphore imageAvailableSemaphore, Fence inFlightFence) {
			GraphicsCommandBuffer = graphicsCommandBuffer;
			ImageAvailableSemaphore = imageAvailableSemaphore;
			InFlightFence = inFlightFence;
		}
	}
}