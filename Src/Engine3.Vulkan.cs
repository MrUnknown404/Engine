using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using Engine3.Utils;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;

namespace Engine3 {
	public static partial class Engine3 {
		internal static readonly string[] RequiredValidationLayers = [
#if DEBUG
				"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
		];

		internal static readonly string[] RequiredInstanceExtensions = [
				Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
				Vk.ExtDebugUtilsExtensionName,
#endif
		];

		internal static readonly string[] RequiredDeviceExtensions = [ Vk.KhrSwapchainExtensionName, Vk.KhrDynamicRenderingExtensionName, ];

		public static VkInstance? VkInstance { get; private set; }
		public static VkPhysicalDevice[] VkPhysicalDevices { get; private set; } = Array.Empty<VkPhysicalDevice>();

		public static bool WasVulkanSetup { get; private set; }

#if DEBUG
		private static VkDebugUtilsMessengerEXT? vkDebugMessenger;
#endif

		private static void SetupVulkan(GameClient gameClient, Version4 engineVersion) {
#if DEBUG
			if (!VkH.CheckSupportForRequiredValidationLayers(gameClient.VkGetRequiredValidationLayers())) { throw new VulkanException("Requested validation layers are not available"); }
#endif

			VkExtensionProperties[] vkInstanceExtensions = VkH.GetInstanceExtensionProperties();
			if (vkInstanceExtensions.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }

			string[] instanceExtensions = gameClient.VkGetInstanceExtensions();
			if (!VkH.CheckSupportForRequiredInstanceExtensions(vkInstanceExtensions, instanceExtensions)) { throw new VulkanException("Requested instance extensions are not available"); }

#if DEBUG
			VkH.PrintInstanceExtensions(vkInstanceExtensions);
#endif

			VKLoader.Init();

			VkInstance = VkH.CreateVulkanInstance(gameClient, Name, gameClient.Version, engineVersion);
			VKLoader.SetInstance(VkInstance.Value);
			Logger.Info("Created Vulkan instance");

#if DEBUG
			vkDebugMessenger = VkH.CreateDebugMessenger(VkInstance.Value, gameClient);
			Logger.Debug("Created Vulkan Debug Messenger");
#endif

			VkPhysicalDevices = VkH.GetPhysicalDevices(VkInstance.Value);
			Logger.Debug("Created Physical Devices");

			WasVulkanSetup = true;
		}

		private static void VkRender(GameClient gameClient, float delta) {
			foreach (VkWindow window in Windows.OfType<VkWindow>().Where(static w => !w.WasDestroyed)) {
				if (window.ShouldClose) {
					window.DestroyWindow();
					continue;
				}

				VkCommandBuffer vkGraphicsCommandBuffer = window.VkGraphicsCommandBuffers[window.CurrentFrame];
				SwapChain swapChain = window.SwapChain;

				if (VkBeginFrame(vkGraphicsCommandBuffer, swapChain, window, window.CurrentFrame, out uint swapChainImageIndex)) {
					VkRender(vkGraphicsCommandBuffer, swapChain, delta);
					gameClient.VkRender(vkGraphicsCommandBuffer, swapChain, delta);
					VkEndFrame(vkGraphicsCommandBuffer, swapChain, window, window.CurrentFrame, swapChainImageIndex, gameClient.MaxFramesInFlight);
				}
			}
		}

		private static unsafe bool VkBeginFrame(VkCommandBuffer vkCommandBuffer, SwapChain swapChain, VkWindow window, uint currentFrame, out uint swapChainImageIndex) {
			if (window.VkGraphicsPipeline is not { } vkGraphicsPipeline) {
				swapChainImageIndex = 0;
				return false;
			}

			VkDevice vkLogicalDevice = window.LogicalGpu.VkLogicalDevice;

			fixed (VkFence* inFlightPtr = window.VkInFlights) {
				Vk.WaitForFences(vkLogicalDevice, 1, &inFlightPtr[currentFrame], (int)Vk.True, ulong.MaxValue);

				uint tempSwapChainImageIndex;
				VkResult vkResult = Vk.AcquireNextImageKHR(vkLogicalDevice, swapChain.VkSwapChain, ulong.MaxValue, window.VkImageAvailable[currentFrame], VkFence.Zero, &tempSwapChainImageIndex); // TODO 2
				swapChainImageIndex = tempSwapChainImageIndex; // ew

				if (vkResult == VkResult.ErrorOutOfDateKhr) {
					window.RecreateSwapChain();
					return false;
				} else if (vkResult is not VkResult.Success and not VkResult.SuboptimalKhr) { throw new VulkanException("Failed to acquire swap chain image"); }

				Vk.ResetFences(vkLogicalDevice, 1, &inFlightPtr[currentFrame]);
			}

			Vk.ResetCommandBuffer(vkCommandBuffer, 0);

			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = 0, pInheritanceInfo = null, };
			if (Vk.BeginCommandBuffer(vkCommandBuffer, &commandBufferBeginInfo) != VkResult.Success) { throw new VulkanException("Failed to begin recording command buffer"); }

			VkImageMemoryBarrier vkImageMemoryBarrier = new() {
					dstAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutUndefined,
					newLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					image = swapChain.VkImages[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(vkCommandBuffer, VkPipelineStageFlagBits.PipelineStageTopOfPipeBit, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, 0, 0, null, 0, null, 1, &vkImageMemoryBarrier); // TODO use 2?

			Color4<Rgba> clearColor = window.ClearColor;
			VkClearColorValue vkClearColorValue = new();
			vkClearColorValue.float32[0] = clearColor.X;
			vkClearColorValue.float32[1] = clearColor.Y;
			vkClearColorValue.float32[2] = clearColor.Z;
			vkClearColorValue.float32[3] = clearColor.W;

			VkRenderingAttachmentInfo vkRenderingAttachmentInfo = new() {
					imageView = swapChain.VkImageViews[swapChainImageIndex],
					imageLayout = VkImageLayout.ImageLayoutAttachmentOptimalKhr,
					loadOp = VkAttachmentLoadOp.AttachmentLoadOpClear,
					storeOp = VkAttachmentStoreOp.AttachmentStoreOpStore,
					clearValue = new() {
							color = vkClearColorValue,
							// depthStencil =, TODO look into what this is/how it works/if i want this
					},
			};

			VkRenderingInfo vkRenderingInfo = new() { renderArea = new() { offset = new(0, 0), extent = swapChain.VkExtent, }, layerCount = 1, colorAttachmentCount = 1, pColorAttachments = &vkRenderingAttachmentInfo, };

			Vk.CmdBeginRendering(vkCommandBuffer, &vkRenderingInfo);

			Vk.CmdBindPipeline(vkCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, vkGraphicsPipeline);

			return true;
		}

		private static void VkRender(VkCommandBuffer vkCommandBuffer, SwapChain swapChain, float delta) { }

		private static unsafe void VkEndFrame(VkCommandBuffer vkCommandBuffer, SwapChain swapChain, VkWindow window, uint currentFrame, uint swapChainImageIndex, uint maxFramesInFlight) {
			Vk.CmdEndRendering(vkCommandBuffer);

			VkImageMemoryBarrier vkImageMemoryBarrier = new() {
					srcAccessMask = VkAccessFlagBits.AccessColorAttachmentWriteBit,
					oldLayout = VkImageLayout.ImageLayoutColorAttachmentOptimal,
					newLayout = VkImageLayout.ImageLayoutPresentSrcKhr,
					image = swapChain.VkImages[swapChainImageIndex],
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			Vk.CmdPipelineBarrier(vkCommandBuffer, VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, VkPipelineStageFlagBits.PipelineStageBottomOfPipeBit, 0, 0, null, 0, null, 1, &vkImageMemoryBarrier); // TODO use 2?

			if (Vk.EndCommandBuffer(vkCommandBuffer) != VkResult.Success) { throw new VulkanException("Failed to end recording command buffer"); }

			VkSemaphore[] renderFinished = window.VkRenderFinished;
			VkH.SubmitCommandBufferQueue(window.LogicalGpu.VkGraphicsQueue, vkCommandBuffer, window.VkImageAvailable[currentFrame], renderFinished[currentFrame], window.VkInFlights[currentFrame]);

			// present image
			fixed (VkSemaphore* renderFinishedPtr = renderFinished) {
				VkSwapchainKHR vkSwapchain = swapChain.VkSwapChain;
				VkPresentInfoKHR presentInfo = new() { waitSemaphoreCount = 1, pWaitSemaphores = &renderFinishedPtr[currentFrame], swapchainCount = 1, pSwapchains = &vkSwapchain, pImageIndices = &swapChainImageIndex, };

				VkResult vkResult = Vk.QueuePresentKHR(window.LogicalGpu.VkPresentQueue, &presentInfo);
				if (vkResult is VkResult.ErrorOutOfDateKhr or VkResult.SuboptimalKhr || window.WasResized) {
					window.WasResized = false;
					window.RecreateSwapChain();
				} else if (vkResult != VkResult.Success) { throw new VulkanException("Failed to present swap chain image"); }
			}

			window.CurrentFrame = (currentFrame + 1) % maxFramesInFlight;
		}

		private static unsafe void CleanupVulkan() {
			if (VkInstance is not { } vkInstance) { return; }

#if DEBUG
			if (vkDebugMessenger is { } debugMessage) {
				Vk.DestroyDebugUtilsMessengerEXT(vkInstance, debugMessage, null);
				vkDebugMessenger = null;
			}
#endif

			Vk.DestroyInstance(vkInstance, null);
			VkInstance = null;
		}
	}
}