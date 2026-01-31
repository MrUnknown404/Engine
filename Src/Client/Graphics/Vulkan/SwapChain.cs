using System.Diagnostics.CodeAnalysis;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.Vulkan {
	public unsafe class SwapChain : IGraphicsResource {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSwapchainKHR VkSwapChain { get; private set; }
		public VkFormat ImageFormat { get; private set; }
		public VkExtent2D Extent { get; private set; }
		public OpenTK.Graphics.Vulkan.VkImage[] Images { get; private set; }
		public VkImageView[] ImageViews { get; private set; }

		public string DebugName => nameof(SwapChain);
		public bool WasDestroyed { get; private set; }

		private readonly VulkanWindow window;
		private readonly VkPresentModeKHR presentMode;

		public SwapChain(VulkanWindow window, VkPhysicalDevice physicalDevice, VkDevice logicalDevice, QueueFamilyIndices queueFamilyIndices, VkSurfaceKHR surface, VkPresentModeKHR presentMode) {
			this.window = window;

			Toolkit.Window.GetFramebufferSize(window.WindowHandle, out Vector2i framebufferSize);
			CreateSwapChain(physicalDevice, logicalDevice, surface, queueFamilyIndices, framebufferSize, out VkSwapchainKHR vkSwapChain, out VkExtent2D swapChainExtent, out VkFormat swapChainImageFormat, presentMode);

			VkSwapChain = vkSwapChain;
			ImageFormat = swapChainImageFormat;
			Extent = swapChainExtent;
			Images = GetSwapChainImages(logicalDevice, vkSwapChain);
			ImageViews = CreateImageViews(logicalDevice, Images, ImageFormat, VkImageAspectFlagBits.ImageAspectColorBit);
			this.presentMode = presentMode;
		}

		public void Recreate() {
			VkDevice logicalDevice = window.LogicalGpu.LogicalDevice;

			Vk.DeviceWaitIdle(logicalDevice);

			Toolkit.Window.GetFramebufferSize(window.WindowHandle, out Vector2i framebufferSize);
			CreateSwapChain(window.SelectedGpu.PhysicalDevice, logicalDevice, window.Surface, window.SelectedGpu.QueueFamilyIndices, framebufferSize, out VkSwapchainKHR vkSwapChain, out VkExtent2D swapChainExtent,
				out VkFormat swapChainImageFormat, presentMode, oldSwapChain: VkSwapChain);

			Logger.Debug("Recreated swap chain");

			Destroy();

			VkSwapChain = vkSwapChain;
			ImageFormat = swapChainImageFormat;
			Extent = swapChainExtent;
			Images = GetSwapChainImages(logicalDevice, vkSwapChain);
			ImageViews = CreateImageViews(logicalDevice, Images, swapChainImageFormat, VkImageAspectFlagBits.ImageAspectColorBit);

			WasDestroyed = false;
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			VkDevice logicalDevice = window.LogicalGpu.LogicalDevice;

			Vk.DestroySwapchainKHR(logicalDevice, VkSwapChain, null);
			foreach (VkImageView imageView in ImageViews) { Vk.DestroyImageView(logicalDevice, imageView, null); }

			WasDestroyed = true;
		}

		[MustUseReturnValue]
		public static bool QuerySupport(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, out VkSurfaceCapabilities2KHR surfaceCapabilities2, [NotNullWhen(true)] out VkSurfaceFormat2KHR[]? surfaceFormats2,
			[NotNullWhen(true)] out VkPresentModeKHR[]? presentModes) {
			surfaceFormats2 = null;
			presentModes = null;

			VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo = new() { surface = surface, };
			VkSurfaceCapabilities2KHR tempSurfaceCapabilities2 = new();
			Vk.GetPhysicalDeviceSurfaceCapabilities2KHR(physicalDevice, &surfaceInfo, &tempSurfaceCapabilities2);
			surfaceCapabilities2 = tempSurfaceCapabilities2;

			VkSurfaceFormat2KHR[] tempSurfaceFormats2 = GetPhysicalDeviceSurfaceFormats(physicalDevice, surfaceInfo);
			if (tempSurfaceFormats2.Length == 0) { return false; }
			surfaceFormats2 = tempSurfaceFormats2;

			VkPresentModeKHR[] tempPresentModes = GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface);
			if (tempPresentModes.Length == 0) { return false; }
			presentModes = tempPresentModes;

			return true;

			[MustUseReturnValue]
			static VkSurfaceFormat2KHR[] GetPhysicalDeviceSurfaceFormats(VkPhysicalDevice physicalDevice, VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo2) {
				uint formatCount;
				Vk.GetPhysicalDeviceSurfaceFormats2KHR(physicalDevice, &surfaceInfo2, &formatCount, null);
				if (formatCount == 0) { return Array.Empty<VkSurfaceFormat2KHR>(); }

				VkSurfaceFormat2KHR[] surfaceFormats = new VkSurfaceFormat2KHR[formatCount];
				for (int i = 0; i < formatCount; i++) { surfaceFormats[i] = new(); }

				fixed (VkSurfaceFormat2KHR* surfaceFormatsPtr = surfaceFormats) {
					Vk.GetPhysicalDeviceSurfaceFormats2KHR(physicalDevice, &surfaceInfo2, &formatCount, surfaceFormatsPtr);
					return surfaceFormats;
				}
			}

			[MustUseReturnValue]
			static VkPresentModeKHR[] GetPhysicalDeviceSurfacePresentModes(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface) {
				uint presentModeCount;
				Vk.GetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, null);
				if (presentModeCount == 0) { return Array.Empty<VkPresentModeKHR>(); }

				VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
				fixed (VkPresentModeKHR* presentModesPtr = presentModes) {
					Vk.GetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, presentModesPtr);
					return presentModes;
				}
			}
		}

		private static void CreateSwapChain(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkSurfaceKHR surface, QueueFamilyIndices queueFamilyIndices, Vector2i framebufferSize, out VkSwapchainKHR swapChain,
			out VkExtent2D swapChainExtent, out VkFormat swapChainImageFormat, VkPresentModeKHR presentMode = VkPresentModeKHR.PresentModeMailboxKhr, VkSurfaceTransformFlagBitsKHR? surfaceTransform = null,
			VkSwapchainKHR? oldSwapChain = null) {
			if (!QuerySupport(physicalDevice, surface, out VkSurfaceCapabilities2KHR surfaceCapabilities2, out VkSurfaceFormat2KHR[]? surfaceFormats2, out VkPresentModeKHR[]? supportedPresentModes)) {
				throw new Engine3VulkanException("Failed to query swap chain support");
			}

			if (!supportedPresentModes.Contains(presentMode)) { throw new Engine3VulkanException("Surface does not support requested present mode"); }
			if (ChooseSwapSurfaceFormat(surfaceFormats2) is not { } surfaceFormat2) { throw new Engine3VulkanException("Could not find any valid surface formats"); }

			VkSurfaceCapabilitiesKHR surfaceCapabilities = surfaceCapabilities2.surfaceCapabilities;
			swapChainImageFormat = surfaceFormat2.surfaceFormat.format;
			swapChainExtent = ChooseSwapExtent(framebufferSize, surfaceCapabilities);

			// https://vulkan-tutorial.com/Drawing_a_triangle/Presentation/Swap_chain#Creating_the_swap_chain - "Therefore it is recommended to request at least one more image than the minimum"
			uint imageCount = surfaceCapabilities.minImageCount + 1;
			if (surfaceCapabilities.maxImageCount > 0 && imageCount > surfaceCapabilities.maxImageCount) { imageCount = surfaceCapabilities.maxImageCount; }

			VkSharingMode imageSharingMode;
			uint queueFamilyIndexCount;
			uint[] queueFamilyIndicesArray;

			HashSet<uint> hashSet = [ queueFamilyIndices.GraphicsFamily, queueFamilyIndices.TransferFamily, queueFamilyIndices.PresentFamily, ];

			if (hashSet.Count != 1) {
				imageSharingMode = VkSharingMode.SharingModeConcurrent;
				queueFamilyIndexCount = (uint)hashSet.Count;
				queueFamilyIndicesArray = hashSet.ToArray();
			} else {
				imageSharingMode = VkSharingMode.SharingModeExclusive;
				queueFamilyIndexCount = 0;
				queueFamilyIndicesArray = Array.Empty<uint>();
			}

			fixed (uint* pQueueFamilyIndicesPtr = queueFamilyIndicesArray) {
				VkSwapchainCreateInfoKHR createInfo = new() {
						surface = surface,
						imageFormat = swapChainImageFormat,
						imageColorSpace = surfaceFormat2.surfaceFormat.colorSpace,
						imageExtent = swapChainExtent,
						minImageCount = imageCount,
						imageArrayLayers = 1,
						imageUsage = VkImageUsageFlagBits.ImageUsageColorAttachmentBit,
						imageSharingMode = imageSharingMode,
						queueFamilyIndexCount = queueFamilyIndexCount,
						pQueueFamilyIndices = queueFamilyIndicesArray.Length == 0 ? null : pQueueFamilyIndicesPtr,
						preTransform = surfaceTransform ?? surfaceCapabilities.currentTransform,
						compositeAlpha = VkCompositeAlphaFlagBitsKHR.CompositeAlphaOpaqueBitKhr,
						presentMode = presentMode,
						clipped = (int)Vk.True,
						oldSwapchain = oldSwapChain ?? VkSwapchainKHR.Zero,
				};

				VkSwapchainKHR tempSwapChain;
				VkH.CheckIfSuccess(Vk.CreateSwapchainKHR(logicalDevice, &createInfo, null, &tempSwapChain), VulkanException.Reason.CreateSwapChain);
				swapChain = tempSwapChain;
			}

			return;

			[MustUseReturnValue]
			static VkSurfaceFormat2KHR? ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormat2KHR> availableFormats) =>
					availableFormats.Where(static format => format.surfaceFormat is { format: VkFormat.FormatB8g8r8a8Srgb, colorSpace: VkColorSpaceKHR.ColorSpaceSrgbNonlinearKhr, }).Cast<VkSurfaceFormat2KHR?>().FirstOrDefault();

			[MustUseReturnValue]
			static VkExtent2D ChooseSwapExtent(Vector2i framebufferSize, VkSurfaceCapabilitiesKHR surfaceCapabilities) =>
					surfaceCapabilities.currentExtent.width != uint.MaxValue ?
							surfaceCapabilities.currentExtent :
							new() {
									width = Math.Clamp((uint)framebufferSize.X, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width),
									height = Math.Clamp((uint)framebufferSize.Y, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height),
							};
		}

		[MustUseReturnValue]
		private static OpenTK.Graphics.Vulkan.VkImage[] GetSwapChainImages(VkDevice logicalDevice, VkSwapchainKHR swapChain) {
			uint swapChainImageCount;
			Vk.GetSwapchainImagesKHR(logicalDevice, swapChain, &swapChainImageCount, null);

			OpenTK.Graphics.Vulkan.VkImage[] swapChainImages = new OpenTK.Graphics.Vulkan.VkImage[swapChainImageCount];
			fixed (OpenTK.Graphics.Vulkan.VkImage* swapChainImagesPtr = swapChainImages) {
				VkH.CheckIfSuccess(Vk.GetSwapchainImagesKHR(logicalDevice, swapChain, &swapChainImageCount, swapChainImagesPtr), VulkanException.Reason.GetSwapChainImages);
				return swapChainImages;
			}
		}

		[MustUseReturnValue]
		private static VkImageView[] CreateImageViews(VkDevice logicalDevice, OpenTK.Graphics.Vulkan.VkImage[] images, VkFormat imageFormat, VkImageAspectFlagBits aspectMask) {
			VkImageView[] imageViews = new VkImageView[images.Length];

			fixed (VkImageView* imageViewsPtr = imageViews) {
				for (int i = 0; i < images.Length; i++) {
					VkImageViewCreateInfo createInfo = new() {
							image = images[i],
							viewType = VkImageViewType.ImageViewType2d,
							format = imageFormat,
							components = new() {
									r = VkComponentSwizzle.ComponentSwizzleIdentity,
									g = VkComponentSwizzle.ComponentSwizzleIdentity,
									b = VkComponentSwizzle.ComponentSwizzleIdentity,
									a = VkComponentSwizzle.ComponentSwizzleIdentity,
							},
							subresourceRange = new() { aspectMask = aspectMask, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
					};

					VkH.CheckIfSuccess(Vk.CreateImageView(logicalDevice, &createInfo, null, &imageViewsPtr[i]), VulkanException.Reason.CreateImageViews, i);
				}
			}

			return imageViews;
		}
	}
}