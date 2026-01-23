using Engine3.Exceptions;
using Engine3.Graphics.Vulkan.Objects;
using Engine3.Utils.Extensions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public abstract unsafe class VkRenderer : IRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected VkWindow Window { get; }
		protected SwapChain SwapChain { get; }

		protected VkCommandPool GraphicsCommandPool { get; }
		protected VkCommandPool TransferCommandPool { get; }

		protected FrameData[] Frames { get; }
		protected VkSemaphore[] RenderFinishedSemaphores { get; }

		protected byte MaxFramesInFlight { get; }
		protected byte CurrentFrame { get; private set; }

		public ulong FrameCount { get; private set; }
		public bool CanRender { get; set; } = true;
		public bool ShouldDestroy { get; set; }
		public bool WasDestroyed { get; private set; }

		protected PhysicalGpu PhysicalGpu => Window.SelectedGpu;
		protected LogicalGpu LogicalGpu => Window.LogicalGpu;
		protected VkPhysicalDevice PhysicalDevice => PhysicalGpu.PhysicalDevice;
		protected VkDevice LogicalDevice => LogicalGpu.LogicalDevice;

		protected VkRenderer(GameClient gameClient, VkWindow window) {
			Window = window;
			MaxFramesInFlight = gameClient.MaxFramesInFlight;

			SwapChain = new(window, window.SelectedGpu.PhysicalDevice, window.LogicalGpu.LogicalDevice, window.SelectedGpu.QueueFamilyIndices, window.Surface, gameClient.PresentMode);
			Logger.Debug("Created swap chain");

			GraphicsCommandPool = CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, PhysicalGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, PhysicalGpu.QueueFamilyIndices.TransferFamily);
			RenderFinishedSemaphores = VkH.CreateSemaphores(LogicalDevice, (uint)SwapChain.Images.Length);

			VkCommandBuffer[] graphicsCommandBuffers = CreateCommandBuffers(LogicalDevice, GraphicsCommandPool, MaxFramesInFlight);
			VkSemaphore[] imageAvailableSemaphores = VkH.CreateSemaphores(LogicalDevice, MaxFramesInFlight);
			VkFence[] inFlightFences = VkH.CreateFences(LogicalDevice, MaxFramesInFlight);

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(LogicalDevice, new(LogicalDevice, GraphicsCommandPool, graphicsCommandBuffers[i]), imageAvailableSemaphores[i], inFlightFences[i]); }
		}

		public abstract void Setup();

		/// <summary>
		/// Wait for the previous frame to finish
		/// Acquire an image from the swap chain
		/// Record a command buffer which draws the scene onto that image
		/// Submit the recorded command buffer
		/// Present the swap chain image
		/// </summary>
		public virtual void Render(float delta) {
			FrameData frameData = Frames[CurrentFrame];

			if (AcquireNextImage(frameData, out uint swapChainImageIndex)) {
				BeginFrame(frameData, swapChainImageIndex);
				RecordCommandBuffer(frameData.GraphicsCommandBuffer, delta);
				UpdateUniformBuffer(delta);
				EndFrame(frameData, swapChainImageIndex);
				PresentFrame(swapChainImageIndex);
			}

			FrameCount++;
		}

		protected bool AcquireNextImage(FrameData frameData, out uint swapChainImageIndex) {
			VkFence inFlightFence = frameData.InFlightFence;

			// not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			// vulkan-tutorial.com waits for all
			// vkguide.dev waits for current
			Vk.WaitForFences(LogicalDevice, 1, &inFlightFence, (int)Vk.True, ulong.MaxValue);

			uint tempSwapChainImageIndex;
			VkResult vkResult = Vk.AcquireNextImageKHR(LogicalDevice, SwapChain.VkSwapChain, ulong.MaxValue, frameData.ImageAvailableSemaphore, VkFence.Zero, &tempSwapChainImageIndex);
			swapChainImageIndex = tempSwapChainImageIndex;

			if (vkResult == VkResult.ErrorOutOfDateKhr) {
				SwapChain.Recreate();
				return false;
			} else if (vkResult is not VkResult.Success and not VkResult.SuboptimalKhr) { throw new VulkanException("Failed to acquire next swap chain image"); }

			Vk.ResetFences(LogicalDevice, 1, &inFlightFence);

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
		protected void BeginFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.ResetCommandBuffer();

			if (graphicsCommandBuffer.BeginCommandBuffer() != VkResult.Success) { throw new VulkanException("Failed to begin recording command buffer"); }

			graphicsCommandBuffer.CmdBeginPipelineBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdBeginRendering(SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], Window.ClearColor.ToVkClearColorValue());
		}

		protected abstract void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta);
		protected virtual void UpdateUniformBuffer(float delta) { }

		/// <summary>
		/// Order of what vulkan methods are called here
		/// <code>
		/// vkCmdEndRendering
		/// vkCmdPipelineBarrier (End)
		/// vkEndCommandBuffer
		/// vkSubmitQueue
		/// </code>
		/// </summary>
		protected void EndFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.CmdEndRendering();
			graphicsCommandBuffer.CmdEndPipelineBarrier(SwapChain.Images[swapChainImageIndex]);

			if (graphicsCommandBuffer.EndCommandBuffer() != VkResult.Success) { throw new VulkanException("Failed to end recording command buffer"); }

			graphicsCommandBuffer.SubmitQueue(LogicalGpu.GraphicsQueue, frameData.ImageAvailableSemaphore, RenderFinishedSemaphores[swapChainImageIndex], frameData.InFlightFence);
		}

		protected void PresentFrame(uint swapChainImageIndex) {
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

		protected abstract void Cleanup();

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Vk.DeviceWaitIdle(LogicalDevice);
			Cleanup();

			Vk.DestroyCommandPool(LogicalDevice, TransferCommandPool, null);
			Vk.DestroyCommandPool(LogicalDevice, GraphicsCommandPool, null);

			foreach (VkSemaphore renderFinishedSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(LogicalDevice, renderFinishedSemaphore, null); }

			foreach (FrameData frame in Frames) { frame.Destroy(); }

			SwapChain.Destroy();

			WasDestroyed = true;
		}

		public bool IsSameWindow(Window window) => Window == window;

		[MustUseReturnValue]
		private static VkCommandPool CreateCommandPool(VkDevice logicalDevice, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			VkResult result = Vk.CreateCommandPool(logicalDevice, &commandPoolCreateInfo, null, &commandPool);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create command pool. {result}") : commandPool;
		}

		[Obsolete("Make VkBufferObject.CreateBuffers()")]
		[MustUseReturnValue]
		private static VkCommandBuffer[] CreateCommandBuffers(VkDevice logicalDevice, VkCommandPool commandPool, uint count, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = count, };
			VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];
			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				VkResult result = Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, commandBuffersPtr);
				return result != VkResult.Success ? throw new VulkanException($"Failed to create command buffers. {result}") : commandBuffers;
			}
		}

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