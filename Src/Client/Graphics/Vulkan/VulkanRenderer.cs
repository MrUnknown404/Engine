using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility;
using Engine3.Utility.Extensions;
using ImGuiNET;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Client.Graphics.Vulkan {
	public abstract unsafe class VulkanRenderer : Renderer<VulkanWindow, VulkanGraphicsBackend, VulkanImGuiBackend> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected SwapChain SwapChain { get; }

		protected CommandPool GraphicsCommandPool { get; }
		protected CommandPool TransferCommandPool { get; }

		protected FrameData[] Frames { get; }
		protected VkSemaphore[] RenderFinishedSemaphores { get; }

		protected virtual DepthImage? DepthImage => null;

		protected byte FrameIndex { get; private set; }

		protected SurfaceCapablePhysicalGpu PhysicalGpu => Window.SelectedGpu;
		protected LogicalGpu LogicalGpu => Window.LogicalGpu;
		protected byte MaxFramesInFlight => GraphicsBackend.MaxFramesInFlight;

		protected VulkanRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, VulkanImGuiBackend? imGuiBackend = null) : base(graphicsBackend, window, imGuiBackend) {
			SwapChain = new(window, window.SelectedGpu.PhysicalDevice, window.LogicalGpu.LogicalDevice, window.SelectedGpu.QueueFamilyIndices, window.Surface, graphicsBackend.PresentMode);
			Logger.Debug("Created swap chain");

			GraphicsCommandPool = LogicalGpu.CreateCommandPool(VkCommandPoolCreateFlagBits.CommandPoolCreateResetCommandBufferBit, PhysicalGpu.QueueFamilyIndices.GraphicsFamily);
			TransferCommandPool = LogicalGpu.CreateCommandPool(VkCommandPoolCreateFlagBits.CommandPoolCreateTransientBit, PhysicalGpu.QueueFamilyIndices.TransferFamily);
			RenderFinishedSemaphores = LogicalGpu.CreateSemaphores((uint)SwapChain.Images.Length);

			GraphicsCommandBuffer[] graphicsCommandBuffers = LogicalGpu.CreateGraphicsCommandBuffers(GraphicsCommandPool.VkCommandPool, MaxFramesInFlight);
			VkSemaphore[] imageAvailableSemaphores = LogicalGpu.CreateSemaphores(MaxFramesInFlight);
			VkFence[] inFlightFences = LogicalGpu.CreateFences(MaxFramesInFlight);

			VkDevice logicalDevice = LogicalGpu.LogicalDevice;

			Frames = new FrameData[MaxFramesInFlight];
			for (int i = 0; i < MaxFramesInFlight; i++) { Frames[i] = new(logicalDevice, graphicsCommandBuffers[i], imageAvailableSemaphores[i], inFlightFences[i]); }
		}

		public override void Setup() => ImGuiBackend?.Setup(TransferCommandPool.VkCommandPool, SwapChain.ImageFormat);

		/// <summary>
		/// Wait for the previous frame to finish
		/// Acquire an image from the swap chain
		/// Record a command buffer which draws the scene onto that image
		/// Submit the recorded command buffer
		/// Present the swap chain image
		/// </summary>
		protected internal override void Render(float delta) {
			LogicalGpu.TryCleanupResources(); // TODO don't destroy every frame?

			FrameData frameData = Frames[FrameIndex];
			if (AcquireNextImage(frameData, out uint swapChainImageIndex)) {
				// copy buffers
				ImDrawDataPtr imDrawData = null;
				bool shouldDrawImGui = false;

				if (ImGuiBackend != null) {
					shouldDrawImGui = ImGuiBackend.NewFrame(out imDrawData);
					if (shouldDrawImGui) { ImGuiBackend.UpdateBuffers(imDrawData); }
				}

				CopyUniformBuffers(delta);

				// draw
				BeginFrame(frameData, swapChainImageIndex);

				GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;
				RecordCommandBuffer(graphicsCommandBuffer, delta);

				if (ImGuiBackend != null && shouldDrawImGui) { ImGuiBackend.RecordCommandBuffer(graphicsCommandBuffer, FrameIndex, imDrawData); }

				EndFrame(frameData, swapChainImageIndex);
				SubmitQueue(frameData.ImageAvailableSemaphore, [ graphicsCommandBuffer.VkCommandBuffer, ], swapChainImageIndex, frameData.InFlightFence);
				PresentFrame(swapChainImageIndex);
			}
		}

		protected virtual bool AcquireNextImage(FrameData frameData, out uint swapChainImageIndex) {
			VkDevice logicalDevice = LogicalGpu.LogicalDevice;
			VkFence inFlightFence = frameData.InFlightFence;

			// not sure if i'm supposed to wait for all fences or just the current one. vulkan-tutorial.com & vkguide.dev differ. i should probably read the docs
			// vulkan-tutorial.com waits for all
			// vkguide.dev waits for current
			Vk.WaitForFences(logicalDevice, 1, &inFlightFence, (int)Vk.True, ulong.MaxValue);

			VkResult result = SwapChain.AcquireNextImage(frameData.ImageAvailableSemaphore, out swapChainImageIndex);

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
		protected virtual void BeginFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.ResetCommandBuffer();

			VkH.CheckIfSuccess(graphicsCommandBuffer.BeginCommandBuffer(0), VulkanException.Reason.BeginCommandBuffer);

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetBeginPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			graphicsCommandBuffer.CmdBeginRendering(SwapChain.Extent, SwapChain.ImageViews[swapChainImageIndex], DepthImage?.Image.ImageView, Window.ClearColor.ToVkClearColorValue(), new(1, 0));
		}

		protected abstract void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta);

		protected virtual void CopyUniformBuffers(float delta) { }

		/// <summary>
		/// Order of what vulkan methods are called here
		/// <code>
		/// vkCmdEndRendering
		/// vkCmdPipelineBarrier (End)
		/// vkEndCommandBuffer
		/// </code>
		/// </summary>
		protected virtual void EndFrame(FrameData frameData, uint swapChainImageIndex) {
			GraphicsCommandBuffer graphicsCommandBuffer = frameData.GraphicsCommandBuffer;

			graphicsCommandBuffer.CmdEndRendering();

			VkImageMemoryBarrier2 imageMemoryBarrier2 = GetEndPipelineBarrierImageMemoryBarrier(SwapChain.Images[swapChainImageIndex]);
			graphicsCommandBuffer.CmdPipelineBarrier(new() { imageMemoryBarrierCount = 1, pImageMemoryBarriers = &imageMemoryBarrier2, });

			VkH.CheckIfSuccess(graphicsCommandBuffer.EndCommandBuffer(), VulkanException.Reason.EndCommandBuffer);
		}

		protected virtual void SubmitQueue(VkSemaphore waitSemaphore, VkCommandBuffer[] commandBuffers, uint swapChainImageIndex, VkFence fence) {
			VkPipelineStageFlagBits* waitStages = stackalloc VkPipelineStageFlagBits[] { VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, };
			VkSemaphore signalSemaphore = RenderFinishedSemaphores[swapChainImageIndex];

			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				VkSubmitInfo a = new() {
						waitSemaphoreCount = 1,
						pWaitSemaphores = &waitSemaphore,
						pWaitDstStageMask = waitStages,
						commandBufferCount = (uint)commandBuffers.Length,
						pCommandBuffers = commandBuffersPtr,
						signalSemaphoreCount = 1,
						pSignalSemaphores = &signalSemaphore,
				};

				Vk.QueueSubmit(LogicalGpu.GraphicsQueue, 1, &a, fence);
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

		[Obsolete] // TODO move elsewhere
		[MustUseReturnValue]
		protected VulkanImage CreateImageAndCopyUsingStaging(string debugName, string fileLocation, string fileExtension, byte texChannels, VkFormat imageFormat, Assembly assembly,
			VkImageTiling imageTiling = VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits usageFlags = VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits aspectMask = VkImageAspectFlagBits.ImageAspectColorBit) {
			using (StbiImage stbiImage = AssetH.LoadImage(fileLocation, fileExtension, texChannels, assembly)) {
				VulkanImage image = LogicalGpu.CreateImage(debugName, (uint)stbiImage.Width, (uint)stbiImage.Height, imageFormat, imageTiling, usageFlags, aspectMask);
				image.CopyUsingStaging(TransferCommandPool.VkCommandPool, LogicalGpu.TransferQueue, stbiImage);
				return image;
			}
		}

		[Obsolete] // TODO move elsewhere
		[MustUseReturnValue]
		protected VulkanImage CreateImageAndCopyUsingStaging(string debugName, uint width, uint height, byte texChannels, VkFormat imageFormat, byte* data, VkImageTiling imageTiling = VkImageTiling.ImageTilingOptimal,
			VkImageUsageFlagBits usageFlags = VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits aspectMask = VkImageAspectFlagBits.ImageAspectColorBit) {
			VulkanImage image = LogicalGpu.CreateImage(debugName, width, height, imageFormat, imageTiling, usageFlags, aspectMask);
			image.CopyUsingStaging(TransferCommandPool.VkCommandPool, LogicalGpu.TransferQueue, width, height, texChannels, data);
			return image;
		}

		[Obsolete] // TODO move elsewhere
		protected void CopyToBuffers(ICollection<CopyToBufferInfo> copyToInfos) {
			List<byte> newData = new();
			foreach (CopyToBufferInfo copyToInfo in copyToInfos) {
				copyToInfo.SrcOffset = (ulong)newData.Count;
				newData.AddRange(copyToInfo.Data);
			}

			ulong bufferSize = (ulong)newData.Count;

			VulkanBuffer stagingBuffer = LogicalGpu.CreateBuffer("Temporary Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize);

			stagingBuffer.Copy(CollectionsMarshal.AsSpan(newData));

			TransferCommandBuffer transferCommandBuffer = LogicalGpu.CreateTransferCommandBuffer(TransferCommandPool.VkCommandPool);

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			foreach (CopyToBufferInfo copyToInfo in copyToInfos) {
				transferCommandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, copyToInfo.Buffer.Buffer, (ulong)copyToInfo.Data.LongLength, copyToInfo.SrcOffset, copyToInfo.DstOffset); //
			}

			transferCommandBuffer.EndCommandBuffer();

			VkQueue queue = LogicalGpu.TransferQueue;
			transferCommandBuffer.SubmitQueue(queue);

			LogicalGpu.EnqueueDestroy(transferCommandBuffer);
			LogicalGpu.EnqueueDestroy(stagingBuffer);
		}

		[Obsolete] // TODO move elsewhere
		protected void CopyToBuffer(ICollection<CopyToInfo> copyToInfos, VulkanBuffer buffer) {
			List<byte> newData = new();
			foreach (CopyToInfo copyToInfo in copyToInfos) {
				copyToInfo.SrcOffset = (ulong)newData.Count;
				newData.AddRange(copyToInfo.Data);
			}

			ulong bufferSize = (ulong)newData.Count;

			VulkanBuffer stagingBuffer = LogicalGpu.CreateBuffer("Temporary Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize);

			stagingBuffer.Copy(CollectionsMarshal.AsSpan(newData));

			TransferCommandBuffer transferCommandBuffer = LogicalGpu.CreateTransferCommandBuffer(TransferCommandPool.VkCommandPool);

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);

			foreach (CopyToInfo copyToInfo in copyToInfos) {
				transferCommandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, buffer.Buffer, (ulong)copyToInfo.Data.LongLength, copyToInfo.SrcOffset, copyToInfo.DstOffset); //
			}

			transferCommandBuffer.EndCommandBuffer();

			VkQueue queue = LogicalGpu.TransferQueue;
			transferCommandBuffer.SubmitQueue(queue);

			LogicalGpu.EnqueueDestroy(transferCommandBuffer);
			LogicalGpu.EnqueueDestroy(stagingBuffer);
		}

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

		protected class CopyToInfo {
			public byte[] Data { get; } // TODO byte[] or byte* ?

			public ulong DstOffset { get; }
			public ulong SrcOffset { get; internal set; }

			public CopyToInfo(byte[] data, ulong dstOffset = 0) {
				Data = data;
				DstOffset = dstOffset;
			}

			public static CopyToInfo Of<T>(ReadOnlySpan<T> data, ulong dstOffset = 0) where T : unmanaged {
				ulong byteSize = (ulong)(sizeof(T) * data.Length);

				byte[] bytes = new byte[byteSize];
				fixed (byte* bytesPtr = bytes) {
					fixed (T* dataPtr = data) { Buffer.MemoryCopy(dataPtr, bytesPtr, byteSize, byteSize); }
				}

				return new(bytes, dstOffset);
			}
		}

		protected class CopyToBufferInfo : CopyToInfo {
			public VulkanBuffer Buffer { get; }

			public CopyToBufferInfo(byte[] data, VulkanBuffer buffer, ulong dstOffset = 0) : base(data, dstOffset) => Buffer = buffer;

			public static CopyToBufferInfo Of<T>(ReadOnlySpan<T> data, VulkanBuffer buffer, ulong dstOffset = 0) where T : unmanaged {
				ulong byteSize = (ulong)(sizeof(T) * data.Length);

				byte[] bytes = new byte[byteSize];
				fixed (byte* bytesPtr = bytes) {
					fixed (T* dataPtr = data) { System.Buffer.MemoryCopy(dataPtr, bytesPtr, byteSize, byteSize); }
				}

				return new(bytes, buffer, dstOffset);
			}
		}
	}
}