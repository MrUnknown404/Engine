using Engine3.Exceptions;
using Engine3.Utils.Extensions;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public abstract unsafe class VkRenderer : Renderer {
		protected VkWindow Window { get; }

		protected VkCommandPool GraphicsCommandPool { get; }
		protected VkCommandPool TransferCommandPool { get; }

		protected FrameData[] Frames { get; }
		public VkSemaphore[] RenderFinishedSemaphores { get; }

		protected FrameData CurrentFrameData => Frames[CurrentFrame];
		protected GraphicsCommandBuffer CurrentGraphicsCommandBuffer => CurrentFrameData.GraphicsCommandBuffer;
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
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(LogicalDevice, new(LogicalDevice, GraphicsCommandPool, graphicsCommandBuffers[i]), imageAvailableSemaphores[i], inFlightFences[i]); }
		}

		/// <summary>
		/// Wait for the previous frame to finish
		/// Acquire an image from the swap chain
		/// Record a command buffer which draws the scene onto that image
		/// Submit the recorded command buffer
		/// Present the swap chain image
		/// </summary>
		protected override void DrawFrame(float delta) {
			if (!CanRender) { return; }

			if (AcquireNextImage(out uint swapChainImageIndex)) {
				BeginFrame(CurrentGraphicsCommandBuffer, swapChainImageIndex);
				RecordCommandBuffer(CurrentGraphicsCommandBuffer, delta);
				UpdateUniformBuffer(delta);
				EndFrame(CurrentGraphicsCommandBuffer, swapChainImageIndex);
				PresentFrame(swapChainImageIndex);
			}
		}

		protected bool AcquireNextImage(out uint swapChainImageIndex) {
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

		/// <summary>
		/// Order of what vulkan methods are called here
		/// <code>
		/// vkResetCommandBuffer
		/// vkBeginCommandBuffer
		/// vkCmdPipelineBarrier (Begin)
		/// vkCmdBeginRendering
		/// </code>
		/// </summary>
		protected void BeginFrame(GraphicsCommandBuffer graphicsCommandBuffer, uint swapChainImageIndex) {
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
		protected void EndFrame(GraphicsCommandBuffer graphicsCommandBuffer, uint swapChainImageIndex) {
			graphicsCommandBuffer.CmdEndRendering();
			graphicsCommandBuffer.CmdEndPipelineBarrier(SwapChain.Images[swapChainImageIndex]);

			if (graphicsCommandBuffer.EndCommandBuffer() != VkResult.Success) { throw new VulkanException("Failed to end recording command buffer"); }

			graphicsCommandBuffer.SubmitQueue(LogicalGpu.GraphicsQueue, CurrentImageAvailableSemaphore, RenderFinishedSemaphores[swapChainImageIndex], CurrentInFlightFence);
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

		protected override void Destroy() {
			Vk.DestroyCommandPool(LogicalDevice, TransferCommandPool, null);
			Vk.DestroyCommandPool(LogicalDevice, GraphicsCommandPool, null);

			foreach (VkSemaphore renderFinishedSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(LogicalDevice, renderFinishedSemaphore, null); }

			foreach (FrameData frame in Frames) { frame.Destroy(); }
		}

		protected class FrameData {
			public GraphicsCommandBuffer GraphicsCommandBuffer { get; }
			public VkSemaphore ImageAvailableSemaphore { get; }
			public VkFence InFlightFence { get; }

			private readonly VkDevice logicalDevice;

			public FrameData(VkDevice logicalDevice, GraphicsCommandBuffer graphicsCommandBuffer, VkSemaphore imageAvailableSemaphore, VkFence inFlightFence) {
				GraphicsCommandBuffer = graphicsCommandBuffer;
				ImageAvailableSemaphore = imageAvailableSemaphore;
				InFlightFence = inFlightFence;
				this.logicalDevice = logicalDevice;
			}

			public void Destroy() {
				Vk.DestroySemaphore(logicalDevice, ImageAvailableSemaphore, null);
				Vk.DestroyFence(logicalDevice, InFlightFence, null);
			}
		}
	}
}