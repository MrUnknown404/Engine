using System.Reflection;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility;
using Engine3.Utility.Extensions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

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

		protected SurfaceCapablePhysicalGpu PhysicalGpu => Window.SelectedGpu;
		protected LogicalGpu LogicalGpu => Window.LogicalGpu;
		public byte MaxFramesInFlight => GraphicsBackend.MaxFramesInFlight;

		// TODO do i want these per renderer or application wide?
		private readonly ResourceManager<DescriptorPool> descriptorPoolManager = new();
		private readonly ResourceManager<TextureSampler> samplerManager = new();

		private readonly NamedResourceManager<GraphicsPipeline> graphicsPipelineManager = new();
		private readonly NamedResourceManager<VulkanBuffer> bufferManager = new();
		private readonly NamedResourceManager<UniformBuffers> uniformBufferManager = new();
		private readonly NamedResourceManager<VulkanImage> imageManager = new();

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
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(logicalDevice, new(logicalDevice, GraphicsCommandPool, graphicsCommandBuffers[i]), imageAvailableSemaphores[i], inFlightFences[i]); }

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
			graphicsPipelineManager.TryCleanup();
			bufferManager.TryCleanup();
			uniformBufferManager.TryCleanup();
			imageManager.TryCleanup();
			samplerManager.TryCleanup();

			FrameData frameData = Frames[FrameIndex];
			if (AcquireNextImage(frameData, out uint swapChainImageIndex)) {
				CopyUniformBuffers(delta);

				BeginFrame(frameData, swapChainImageIndex);

				GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;
				RecordCommandBuffer(graphicsCommandBuffer, delta);

				EndFrame(frameData, swapChainImageIndex);
				SubmitQueue(frameData.ImageAvailableSemaphore, [ graphicsCommandBuffer.VkCommandBuffer, ], swapChainImageIndex, frameData.InFlightFence);
				PresentFrame(swapChainImageIndex);
			}

			FrameCount++;
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

			VkH.CheckIfSuccess(graphicsCommandBuffer.BeginCommandBuffer(0), VulkanException.Reason.BeginCommandBuffer);

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetBeginPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			graphicsCommandBuffer.CmdBeginRendering(SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], DepthImage.Image.ImageView, Window.ClearColor.ToVkClearColorValue(), new(1, 0));
		}

		protected abstract void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta);

		protected virtual VkCommandBuffer[] ProvideAdditionalCommandBuffers(float delta) => Array.Empty<VkCommandBuffer>();
		protected virtual void CopyUniformBuffers(float delta) { }

		/// <summary>
		/// Order of what vulkan methods are called here
		/// <code>
		/// vkCmdEndRendering
		/// vkCmdPipelineBarrier (End)
		/// vkEndCommandBuffer
		/// </code>
		/// </summary>
		protected void EndFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.CmdEndRendering();

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetEndPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			VkH.CheckIfSuccess(graphicsCommandBuffer.EndCommandBuffer(), VulkanException.Reason.EndCommandBuffer);
		}

		protected void SubmitQueue(VkSemaphore waitSemaphore, VkCommandBuffer[] commandBuffers, uint swapChainImageIndex, VkFence fence) {
			VkPipelineStageFlagBits[] waitStages = [ VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, ];

			VkSemaphore signalSemaphore = RenderFinishedSemaphores[swapChainImageIndex];

			fixed (VkPipelineStageFlagBits* waitStagesPtr = waitStages) {
				fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
					VkSubmitInfo a = new() {
							waitSemaphoreCount = 1,
							pWaitSemaphores = &waitSemaphore,
							pWaitDstStageMask = waitStagesPtr,
							commandBufferCount = (uint)commandBuffers.Length,
							pCommandBuffers = commandBuffersPtr,
							signalSemaphoreCount = 1,
							pSignalSemaphores = &signalSemaphore,
					};

					Vk.QueueSubmit(LogicalGpu.GraphicsQueue, 1, &a, fence);
				}
			}
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

		[MustUseReturnValue]
		protected DescriptorPool CreateDescriptorPool(VkDescriptorType[] descriptorSetTypes, uint count) {
			DescriptorPool descriptorPool = new(LogicalGpu.LogicalDevice, count, descriptorSetTypes, MaxFramesInFlight);
			descriptorPoolManager.Add(descriptorPool);
			return descriptorPool;
		}

		[MustUseReturnValue]
		protected GraphicsPipeline CreateGraphicsPipeline(GraphicsPipeline.Settings settings) {
			GraphicsPipeline graphicsPipeline = LogicalGpu.CreateGraphicsPipeline(settings);
			graphicsPipelineManager.Add(graphicsPipeline);
			return graphicsPipeline;
		}

		[MustUseReturnValue]
		protected VulkanBuffer CreateBuffer(string debugName, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) {
			VulkanBuffer buffer = LogicalGpu.CreateBuffer(debugName, bufferUsageFlags, memoryPropertyFlags, bufferSize);
			bufferManager.Add(buffer);
			return buffer;
		}

		[MustUseReturnValue]
		protected UniformBuffers CreateUniformBuffers(string debugName, ulong bufferSize) {
			UniformBuffers buffer = LogicalGpu.CreateUniformBuffers(debugName, this, bufferSize);
			uniformBufferManager.Add(buffer);
			return buffer;
		}

		[MustUseReturnValue]
		protected VulkanImage CreateImage(string debugName, uint width, uint height, VkFormat imageFormat) =>
				CreateImage(debugName, width, height, imageFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits.ImageAspectColorBit);

		[MustUseReturnValue]
		protected VulkanImage CreateImage(string debugName, uint width, uint height, VkFormat imageFormat, VkImageTiling imageTiling, VkImageUsageFlagBits usageFlags, VkImageAspectFlagBits aspectMask) {
			VulkanImage image = LogicalGpu.CreateImage(debugName, width, height, imageFormat, imageTiling, usageFlags, aspectMask);
			imageManager.Add(image);
			return image;
		}

		[MustUseReturnValue]
		protected VulkanImage CreateImageAndCopyUsingStaging(string debugName, string fileLocation, string fileExtension, uint width, uint height, byte texChannels, VkFormat imageFormat, Assembly assembly) {
			using (StbiImage stbiImage = AssetH.LoadImage(fileLocation, fileExtension, texChannels, assembly)) {
				VulkanImage image = CreateImage(debugName, width, height, imageFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits.ImageAspectColorBit);
				image.Copy(TransferCommandPool, LogicalGpu.TransferQueue, stbiImage);
				return image;
			}
		}

		[MustUseReturnValue]
		protected TextureSampler CreateSampler(TextureSampler.Settings settings) {
			TextureSampler sampler = LogicalGpu.CreateSampler(settings);
			samplerManager.Add(sampler);
			return sampler;
		}

		protected internal void DestroyResource(GraphicsPipeline graphicsPipeline) => graphicsPipelineManager.Destroy(graphicsPipeline);
		protected internal void DestroyResource(VulkanBuffer buffer) => bufferManager.Destroy(buffer);
		protected internal void DestroyResource(UniformBuffers buffer) => uniformBufferManager.Destroy(buffer);
		protected internal void DestroyResource(VulkanImage image) => imageManager.Destroy(image);
		protected internal void DestroyResource(TextureSampler sampler) => samplerManager.Destroy(sampler);

		protected virtual void OnSwapchainInvalid() {
			SwapChain.Recreate();
			DepthImage.Recreate(SwapChain.Extent);
		}

		protected override void PrepareCleanup() => Vk.DeviceWaitIdle(LogicalGpu.LogicalDevice);

		protected override void Cleanup() {
			VkDevice logicalDevice = LogicalGpu.LogicalDevice;

			Vk.DeviceWaitIdle(logicalDevice);

			bufferManager.CleanupAll();
			uniformBufferManager.CleanupAll();
			imageManager.CleanupAll();
			samplerManager.CleanupAll();

			graphicsPipelineManager.CleanupAll();

			descriptorPoolManager.CleanupAll();

			DepthImage.Destroy();

			Vk.DestroyCommandPool(logicalDevice, TransferCommandPool, null);
			Vk.DestroyCommandPool(logicalDevice, GraphicsCommandPool, null);

			foreach (VkSemaphore renderFinishedSemaphore in RenderFinishedSemaphores) { Vk.DestroySemaphore(logicalDevice, renderFinishedSemaphore, null); }
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

		[MustUseReturnValue]
		private static VkCommandPool CreateCommandPool(VkDevice logicalDevice, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			VkH.CheckIfSuccess(Vk.CreateCommandPool(logicalDevice, &commandPoolCreateInfo, null, &commandPool), VulkanException.Reason.CreateCommandPool);
			return commandPool;
		}

		[Obsolete($"Make {nameof(VulkanBuffer)}.CreateBuffers()")] // TODO make CreateBuffers()
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