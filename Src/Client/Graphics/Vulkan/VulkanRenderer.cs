using Engine3.Exceptions;
using Engine3.Utility;
using Engine3.Utility.Extensions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	public abstract unsafe class VulkanRenderer : Renderer<VulkanWindow, VulkanGraphicsBackend> {
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
		public byte MaxFramesInFlight => GraphicsBackend.MaxFramesInFlight;

		private readonly List<GraphicsPipeline> graphicsPipelines = new();

		private readonly Queue<DescriptorPool> descriptorPoolDestroyQueue = new();
		private readonly Queue<GraphicsPipeline> graphicsPipelineDestroyQueue = new();

		protected VulkanRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window) : base(graphicsBackend, window) {
			SwapChain = new(window, window.SelectedGpu.PhysicalDevice, window.LogicalGpu.LogicalDevice, window.SelectedGpu.QueueFamilyIndices, window.Surface, graphicsBackend.PresentMode);
			Logger.Debug("Created swap chain");

			VkDevice logicalDevice = LogicalGpu.LogicalDevice;

			GraphicsCommandPool = CreateCommandPool(logicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, PhysicalGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = CreateCommandPool(logicalDevice, VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, PhysicalGpu.QueueFamilyIndices.TransferFamily);
			RenderFinishedSemaphores = VkH.CreateSemaphores(logicalDevice, (uint)SwapChain.Images.Length);

			VkCommandBuffer[] graphicsCommandBuffers = CreateCommandBuffers(logicalDevice, GraphicsCommandPool, MaxFramesInFlight);
			VkSemaphore[] imageAvailableSemaphores = VkH.CreateSemaphores(logicalDevice, MaxFramesInFlight);
			VkFence[] inFlightFences = VkH.CreateFences(logicalDevice, MaxFramesInFlight);

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) {
				Frames[i] = new(logicalDevice, new(logicalDevice, GraphicsCommandPool, graphicsCommandBuffers[i], window.LogicalGpu.GraphicsQueue), imageAvailableSemaphores[i], inFlightFences[i]);
			}

			DepthImage = LogicalGpu.CreateDepthImage($"{nameof(VulkanRenderer)} Depth Image", TransferCommandPool, SwapChain.Extent);
		}

		/// <summary>
		/// Wait for the previous frame to finish
		/// Acquire an image from the swap chain
		/// Record a command buffer which draws the scene onto that image
		/// Submit the recorded command buffer
		/// Present the swap chain image
		/// </summary>
		protected internal override void Render(float delta) {
			TryDestroyGraphicsPipelines();

			FrameData frameData = Frames[FrameIndex];
			if (AcquireNextImage(frameData, out uint swapChainImageIndex)) {
				BeginFrame(frameData, swapChainImageIndex);
				RecordCommandBuffer(frameData.GraphicsCommandBuffer, delta);
				CopyUniformBuffers(delta);
				EndFrame(frameData, swapChainImageIndex);
				PresentFrame(swapChainImageIndex);
			}

			FrameCount++;

			return;

			void TryDestroyGraphicsPipelines() {
				if (graphicsPipelineDestroyQueue.Count != 0) {
					Vk.DeviceWaitIdle(LogicalGpu.LogicalDevice);

					while (graphicsPipelineDestroyQueue.TryDequeue(out GraphicsPipeline? graphicsPipeline)) {
						if (graphicsPipelines.Remove(graphicsPipeline)) {
#pragma warning disable CS0618 // Type or member is obsolete
							graphicsPipeline.Destroy();
#pragma warning restore CS0618 // Type or member is obsolete

							break;
						} else { Logger.Error($"Could not find to be destroyed {nameof(GraphicsPipeline)}"); }
					}
				}
			}
		}

		protected bool AcquireNextImage(FrameData frameData, out uint swapChainImageIndex) {
			VkDevice logicalDevice = LogicalGpu.LogicalDevice;
			VkFence inFlightFence = frameData.InFlightFence;

			// not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			// vulkan-tutorial.com waits for all
			// vkguide.dev waits for current
			Vk.WaitForFences(logicalDevice, 1, &inFlightFence, (int)Vk.True, ulong.MaxValue);

			uint tempSwapChainImageIndex;
			VkResult result = Vk.AcquireNextImageKHR(logicalDevice, SwapChain.VkSwapChain, ulong.MaxValue, frameData.ImageAvailableSemaphore, VkFence.Zero, &tempSwapChainImageIndex); // todo 2
			swapChainImageIndex = tempSwapChainImageIndex;

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
		protected void BeginFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.ResetCommandBuffer();

			VkH.CheckIfSuccess(graphicsCommandBuffer.BeginCommandBuffer(), VulkanException.Reason.BeginCommandBuffer);

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetBeginPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			graphicsCommandBuffer.CmdBeginRendering(SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], DepthImage.Image.ImageView, Window.ClearColor.ToVkClearColorValue(), new(1, 0));
		}

		protected abstract void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta);
		protected virtual void CopyUniformBuffers(float delta) { }

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

		protected static VkImageMemoryBarrier2 GetBeginPipelineBarrierImageMemoryBarrier(OpenTK.Graphics.Vulkan.VkImage image) =>
				new() {
						dstAccessMask = VkAccessFlagBits2.Access2ColorAttachmentWriteBit,
						dstStageMask = VkPipelineStageFlagBits2.PipelineStage2TopOfPipeBit | VkPipelineStageFlagBits2.PipelineStage2ColorAttachmentOutputBit,
						oldLayout = VkImageLayout.ImageLayoutUndefined,
						newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
						image = image,
						subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
				};

		protected static VkImageMemoryBarrier2 GetEndPipelineBarrierImageMemoryBarrier(OpenTK.Graphics.Vulkan.VkImage image) =>
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
			DescriptorPool descriptorPool = new(LogicalGpu.LogicalDevice, count, descriptorSetTypes, MaxFramesInFlight);
			descriptorPoolDestroyQueue.Enqueue(descriptorPool);
			return descriptorPool;
		}

		[MustUseReturnValue]
		protected GraphicsPipeline CreateGraphicsPipeline(GraphicsPipeline.Settings settings) {
			GraphicsPipeline graphicsPipeline = LogicalGpu.CreateGraphicsPipeline(settings);
			graphicsPipelines.Add(graphicsPipeline);
			return graphicsPipeline;
		}

		protected internal void DestroyGraphicsPipeline(GraphicsPipeline graphicsPipeline) => graphicsPipelineDestroyQueue.Enqueue(graphicsPipeline);

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

			VkDevice logicalDevice = LogicalGpu.LogicalDevice;

			Vk.DeviceWaitIdle(logicalDevice);
			Cleanup();

#pragma warning disable CS0618 // Type or member is obsolete
			foreach (GraphicsPipeline graphicsPipeline in graphicsPipelines) { graphicsPipeline.Destroy(); }
#pragma warning restore CS0618 // Type or member is obsolete

			while (descriptorPoolDestroyQueue.TryDequeue(out DescriptorPool? descriptorPool)) { descriptorPool.Destroy(); }

			DepthImage.Destroy();

			Vk.DestroyCommandPool(logicalDevice, TransferCommandPool, null);
			Vk.DestroyCommandPool(logicalDevice, GraphicsCommandPool, null);

			foreach (VkSemaphore renderFinishedSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(logicalDevice, renderFinishedSemaphore, null); }
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

		[Obsolete("Make VulkanBuffer.CreateBuffers()")]
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