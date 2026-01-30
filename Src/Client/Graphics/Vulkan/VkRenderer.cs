using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility;
using Engine3.Utility.Extensions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public abstract unsafe class VkRenderer : Renderer<VkWindow, VulkanGraphicsBackend> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected SwapChain SwapChain { get; }

		protected VkCommandPool GraphicsCommandPool { get; }
		protected VkCommandPool TransferCommandPool { get; }

		protected FrameData[] Frames { get; }
		protected VkSemaphore[] RenderFinishedSemaphores { get; }
		protected DepthImage DepthImage { get; }

		public byte FrameIndex { get; private set; }

		protected PhysicalGpu PhysicalGpu => Window.SelectedGpu;
		protected LogicalGpu LogicalGpu => Window.LogicalGpu;
		protected VkPhysicalDevice PhysicalDevice => PhysicalGpu.PhysicalDevice;
		protected VkDevice LogicalDevice => LogicalGpu.LogicalDevice;
		protected VkPhysicalDeviceMemoryProperties PhysicalDeviceMemoryProperties => PhysicalGpu.PhysicalDeviceMemoryProperties2.memoryProperties;
		public byte MaxFramesInFlight => GraphicsBackend.MaxFramesInFlight;

		private readonly Queue<DescriptorPool> descriptorPools = new();

		protected VkRenderer(VulkanGraphicsBackend graphicsBackend, VkWindow window) : base(graphicsBackend, window) {
			SwapChain = new(window, window.SelectedGpu.PhysicalDevice, window.LogicalGpu.LogicalDevice, window.SelectedGpu.QueueFamilyIndices, window.Surface, graphicsBackend.PresentMode);
			Logger.Debug("Created swap chain");

			GraphicsCommandPool = CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, PhysicalGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = CreateCommandPool(LogicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, PhysicalGpu.QueueFamilyIndices.TransferFamily);
			RenderFinishedSemaphores = VkH.CreateSemaphores(LogicalDevice, (uint)SwapChain.Images.Length);

			VkCommandBuffer[] graphicsCommandBuffers = CreateCommandBuffers(LogicalDevice, GraphicsCommandPool, MaxFramesInFlight);
			VkSemaphore[] imageAvailableSemaphores = VkH.CreateSemaphores(LogicalDevice, MaxFramesInFlight);
			VkFence[] inFlightFences = VkH.CreateFences(LogicalDevice, MaxFramesInFlight);

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) {
				Frames[i] = new(LogicalDevice, new(LogicalDevice, GraphicsCommandPool, graphicsCommandBuffers[i], window.LogicalGpu.GraphicsQueue), imageAvailableSemaphores[i], inFlightFences[i]);
			}

			DepthImage = new($"{nameof(VkRenderer)} Depth Image", PhysicalDevice, LogicalDevice, window.SelectedGpu.QueueFamilyIndices, PhysicalDeviceMemoryProperties, TransferCommandPool, window.LogicalGpu.TransferQueue,
				SwapChain.Extent);
		}

		/// <summary>
		/// Wait for the previous frame to finish
		/// Acquire an image from the swap chain
		/// Record a command buffer which draws the scene onto that image
		/// Submit the recorded command buffer
		/// Present the swap chain image
		/// </summary>
		protected internal override void Render(float delta) {
			FrameData frameData = Frames[FrameIndex];

			if (AcquireNextImage(frameData, out uint swapChainImageIndex)) {
				BeginFrame(frameData, swapChainImageIndex);
				RecordCommandBuffer(frameData.GraphicsCommandBuffer, delta);
				CopyUniformBuffer(delta);
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
			VkResult result = Vk.AcquireNextImageKHR(LogicalDevice, SwapChain.VkSwapChain, ulong.MaxValue, frameData.ImageAvailableSemaphore, VkFence.Zero, &tempSwapChainImageIndex); // todo 2
			swapChainImageIndex = tempSwapChainImageIndex;

			if (result == VkResult.ErrorOutOfDateKhr) {
				OnSwapchainInvalid();
				return false;
			} else if (result != VkResult.SuboptimalKhr) { VkH.CheckIfSuccess(result, VulkanException.Reason.AcquireNextImage); }

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
			GraphicsCommandBufferObject graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.ResetCommandBuffer();

			VkH.CheckIfSuccess(graphicsCommandBuffer.BeginCommandBuffer(), VulkanException.Reason.BeginCommandBuffer);

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetBeginPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			graphicsCommandBuffer.CmdBeginRendering(SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], DepthImage.ImageView, Window.ClearColor.ToVkClearColorValue(), new(1, 0));
		}

		protected abstract void RecordCommandBuffer(GraphicsCommandBufferObject graphicsCommandBuffer, float delta);
		protected virtual void CopyUniformBuffer(float delta) { }

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
			GraphicsCommandBufferObject graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.CmdEndRendering();

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetEndPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			VkH.CheckIfSuccess(graphicsCommandBuffer.EndCommandBuffer(), VulkanException.Reason.EndCommandBuffer);

			graphicsCommandBuffer.SubmitQueue(frameData.ImageAvailableSemaphore, RenderFinishedSemaphores[swapChainImageIndex], frameData.InFlightFence);
		}

		protected void PresentFrame(uint swapChainImageIndex) {
			VkSwapchainKHR swapChain = SwapChain.VkSwapChain;
			VkSemaphore renderFinishedSemaphore = RenderFinishedSemaphores[swapChainImageIndex];

			VkPresentInfoKHR presentInfo = new() { waitSemaphoreCount = 1, pWaitSemaphores = &renderFinishedSemaphore, swapchainCount = 1, pSwapchains = &swapChain, pImageIndices = &swapChainImageIndex, };
			VkResult result = Vk.QueuePresentKHR(LogicalGpu.PresentQueue, &presentInfo);

			if (result is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr || Window.WasResized) {
				Window.WasResized = false;
				OnSwapchainInvalid();
			} else { VkH.CheckIfSuccess(result, VulkanException.Reason.QueuePresent); }

			FrameIndex = (byte)((FrameIndex + 1) % MaxFramesInFlight);
		}

		protected static VkImageMemoryBarrier2 GetBeginPipelineBarrierImageMemoryBarrier(VkImage image) =>
				new() {
						dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
						oldLayout = VkImageLayout.ImageLayoutUndefined,
						newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
						image = image,
						subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
				};

		protected static VkImageMemoryBarrier2 GetEndPipelineBarrierImageMemoryBarrier(VkImage image) =>
				new() {
						srcAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
						srcStageMask = VkPipelineStageFlagBits2.PipelineStage2BottomOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
						oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
						newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
						image = image,
						subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
				};

		[MustUseReturnValue]
		protected DescriptorPool CreateDescriptorPool(VkDescriptorType[] descriptorSetTypes, uint count) {
			DescriptorPool descriptorPool = new(LogicalDevice, count, descriptorSetTypes, MaxFramesInFlight);
			descriptorPools.Enqueue(descriptorPool);
			return descriptorPool;
		}

		protected virtual void OnSwapchainInvalid() {
			SwapChain.Recreate();
			DepthImage.Recreate(SwapChain.Extent);
		}

		public override bool IsSameWindow(Window window) => Window == window;

		public override void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			if (!ShouldDestroy) {
				ShouldDestroy = true;
				return;
			}

			ActuallyDestroy();

			WasDestroyed = true;
		}

		internal override void ActuallyDestroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Vk.DeviceWaitIdle(LogicalDevice);
			Cleanup();

			while (descriptorPools.TryDequeue(out DescriptorPool? descriptorPool)) { descriptorPool.Destroy(); }

			DepthImage.Destroy();

			Vk.DestroyCommandPool(LogicalDevice, TransferCommandPool, null);
			Vk.DestroyCommandPool(LogicalDevice, GraphicsCommandPool, null);

			foreach (VkSemaphore renderFinishedSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(LogicalDevice, renderFinishedSemaphore, null); }
			foreach (FrameData frame in Frames) { frame.Destroy(); }

			SwapChain.Destroy();

			WasDestroyed = true;
		}

		[MustUseReturnValue]
		private static VkCommandPool CreateCommandPool(VkDevice logicalDevice, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			VkH.CheckIfSuccess(Vk.CreateCommandPool(logicalDevice, &commandPoolCreateInfo, null, &commandPool), VulkanException.Reason.CreateCommandPool);
			return commandPool;
		}

		[Obsolete("Make VkBuffer.CreateBuffers()")]
		[MustUseReturnValue]
		private static VkCommandBuffer[] CreateCommandBuffers(VkDevice logicalDevice, VkCommandPool commandPool, uint count, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = count, };
			VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];
			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				VkH.CheckIfSuccess(Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, commandBuffersPtr), VulkanException.Reason.AllocateCommandBuffers);
				return commandBuffers;
			}
		}

		protected class FrameData {
			public GraphicsCommandBufferObject GraphicsCommandBuffer { get; }
			public VkSemaphore ImageAvailableSemaphore { get; }
			public VkFence InFlightFence { get; }

			private readonly VkDevice logicalDevice;

			public FrameData(VkDevice logicalDevice, GraphicsCommandBufferObject graphicsCommandBuffer, VkSemaphore imageAvailableSemaphore, VkFence inFlightFence) {
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