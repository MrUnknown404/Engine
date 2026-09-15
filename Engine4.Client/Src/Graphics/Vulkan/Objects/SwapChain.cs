using System.Diagnostics.CodeAnalysis;
using Engine4.IO;
using Engine4.Utility.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class SwapChain {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Vulkan);

	private readonly Window window;
	private readonly BoundPhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;
	private readonly VulkanSurface surface;
	private readonly VkPresentModeKHR presentMode;

	internal VkSwapchainKHR VkSwapChain { get; private set; } // TODO private
	internal VkFormat ImageFormat { get; private set; } // TODO private
	internal VkExtent2D Extent { get; private set; } // TODO private
	internal VkImage[] Images { get; private set; } // TODO private
	internal VkImageView[] ImageViews { get; private set; } // TODO private

	internal SwapChain(Window window, BoundPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VulkanSurface surface, VkPresentModeKHR presentMode) {
		this.window = window;
		this.physicalGpu = physicalGpu;
		this.logicalGpu = logicalGpu;
		this.surface = surface;
		this.presentMode = presentMode;

		VkSwapChain = CreateSwapChain(window, physicalGpu, logicalGpu, surface, presentMode, out VkExtent2D swapChainExtent, out VkFormat swapChainImageFormat);
		ImageFormat = swapChainImageFormat;
		Extent = swapChainExtent;
		Images = GetSwapChainImages(logicalGpu, VkSwapChain);
		ImageViews = CreateImageViews(logicalGpu, Images, ImageFormat, VkImageAspectFlagBits.ImageAspectColorBit);
	}

	internal void Recreate() {
		Vk.DeviceWaitIdle(logicalGpu.VkLogicalDevice);

		VkSwapchainKHR vkSwapChain = CreateSwapChain(window, physicalGpu, logicalGpu, surface, presentMode, out VkExtent2D swapChainExtent, out VkFormat swapChainImageFormat, oldSwapChain: VkSwapChain);
		Logger.Trace("Recreated swap chain");

		Cleanup();

		VkSwapChain = vkSwapChain;
		ImageFormat = swapChainImageFormat;
		Extent = swapChainExtent;
		Images = GetSwapChainImages(logicalGpu, VkSwapChain);
		ImageViews = CreateImageViews(logicalGpu, Images, swapChainImageFormat, VkImageAspectFlagBits.ImageAspectColorBit);
	}

	internal void Cleanup() { // TODO call this
		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;
		Vk.DestroySwapchainKHR(logicalDevice, VkSwapChain, null);
		foreach (VkImageView imageView in ImageViews) { Vk.DestroyImageView(logicalDevice, imageView, null); }
	}

	[MustUseReturnValue]
	private static VkSwapchainKHR CreateSwapChain(Window window, BoundPhysicalGpu physicalGpu, LogicalGpu logicalGpu, VulkanSurface surface, VkPresentModeKHR presentMode, out VkExtent2D swapChainExtent,
		out VkFormat swapChainImageFormat, VkSurfaceTransformFlagBitsKHR? surfaceTransform = null, VkSwapchainKHR? oldSwapChain = null) {
		// check if the surface is swapchain capable
		// TODO shouldn't i check this before i get here?
		if (!QuerySurfaceSupport(physicalGpu, surface, presentMode, out VkSurfaceCapabilities2KHR? surfaceCapabilities2, out VkSurfaceFormat2KHR? surfaceFormat2, out VkPresentModeKHR[]? supportedPresentModes)) {
			throw new Engine4Exception("Failed to query swap chain support");
		}

		Vec2<ushort> frameBufferSize = window.GetFrameBufferSize();

		VkSurfaceCapabilitiesKHR surfaceCapabilities = surfaceCapabilities2.Value.surfaceCapabilities;
		swapChainImageFormat = surfaceFormat2.Value.surfaceFormat.format;
		swapChainExtent = ChooseSwapExtent(frameBufferSize, surfaceCapabilities);

		// https://vulkan-tutorial.com/Drawing_a_triangle/Presentation/Swap_chain#Creating_the_swap_chain - "Therefore it is recommended to request at least one more image than the minimum"
		uint imageCount = surfaceCapabilities.minImageCount + 1;
		if (surfaceCapabilities.maxImageCount > 0 && imageCount > surfaceCapabilities.maxImageCount) { imageCount = surfaceCapabilities.maxImageCount; }

		VkSharingMode imageSharingMode;
		uint queueFamilyIndexCount;
		uint[] queueFamilyIndicesArray;

		QueueFamilyIndices queueFamilyIndices = physicalGpu.QueueFamilyIndices;
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
					surface = surface.VkSurface,
					imageFormat = swapChainImageFormat,
					imageColorSpace = surfaceFormat2.Value.surfaceFormat.colorSpace,
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
					clipped = VkH.True,
					oldSwapchain = oldSwapChain ?? VkSwapchainKHR.Zero,
			};

			VkSwapchainKHR swapChain;
			return Vk.CreateSwapchainKHR(logicalGpu.VkLogicalDevice, &createInfo, null, &swapChain) == VkResult.Success ? swapChain : throw new Exception(); // TODO exception
		}

		[MustUseReturnValue]
		static bool QuerySurfaceSupport(BoundPhysicalGpu physicalGpu, VulkanSurface surface, VkPresentModeKHR presentMode, [NotNullWhen(true)] out VkSurfaceCapabilities2KHR? surfaceCapabilities2,
			[NotNullWhen(true)] out VkSurfaceFormat2KHR? surfaceFormat2, [NotNullWhen(true)] out VkPresentModeKHR[]? presentModes) {
			VkSurfaceKHR vkSurface = surface.VkSurface;
			VkPhysicalDevice vkPhysicalDevice = physicalGpu.VkPhysicalDevice;

			VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo = new() { surface = vkSurface, };
			VkSurfaceCapabilities2KHR tempSurfaceCapabilities2 = new();
			Vk.GetPhysicalDeviceSurfaceCapabilities2KHR(physicalGpu.VkPhysicalDevice, &surfaceInfo, &tempSurfaceCapabilities2);
			surfaceCapabilities2 = tempSurfaceCapabilities2;

			VkSurfaceFormat2KHR[]? supportedSurfaceFormats = GetPhysicalDeviceSurfaceFormats(vkPhysicalDevice, surfaceInfo);
			presentModes = GetPhysicalDeviceSurfacePresentModes(vkPhysicalDevice, vkSurface);

			if (supportedSurfaceFormats == null || presentModes == null) {
				surfaceFormat2 = null;
				return false;
			}

			if (!presentModes.Contains(presentMode)) { throw new Engine4Exception("Surface does not support requested present mode"); }

			surfaceFormat2 = ChooseSwapSurfaceFormat(supportedSurfaceFormats);
			return surfaceFormat2 != null;
		}

		[MustUseReturnValue]
		static VkSurfaceFormat2KHR[]? GetPhysicalDeviceSurfaceFormats(VkPhysicalDevice physicalDevice, VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo2) {
			uint formatCount;
			Vk.GetPhysicalDeviceSurfaceFormats2KHR(physicalDevice, &surfaceInfo2, &formatCount, null);
			if (formatCount == 0) { return Array.Empty<VkSurfaceFormat2KHR>(); }

			VkSurfaceFormat2KHR[] surfaceFormats = new VkSurfaceFormat2KHR[formatCount];
			for (int i = 0; i < formatCount; i++) { surfaceFormats[i] = new(); }

			fixed (VkSurfaceFormat2KHR* surfaceFormatsPtr = surfaceFormats) {
				Vk.GetPhysicalDeviceSurfaceFormats2KHR(physicalDevice, &surfaceInfo2, &formatCount, surfaceFormatsPtr);
				return surfaceFormats.Length == 0 ? null : surfaceFormats;
			}
		}

		[MustUseReturnValue]
		static VkPresentModeKHR[]? GetPhysicalDeviceSurfacePresentModes(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface) {
			uint presentModeCount;
			Vk.GetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, null);
			if (presentModeCount == 0) { return Array.Empty<VkPresentModeKHR>(); }

			VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
			fixed (VkPresentModeKHR* presentModesPtr = presentModes) {
				Vk.GetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, presentModesPtr);
				return presentModes.Length == 0 ? null : presentModes;
			}
		}

		[MustUseReturnValue]
		static VkSurfaceFormat2KHR? ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormat2KHR> availableFormats) { // TODO more control?
			foreach (VkSurfaceFormat2KHR surfaceFormat in availableFormats) {
				if (surfaceFormat.surfaceFormat is { format: VkFormat.FormatB8g8r8a8Srgb, colorSpace: VkColorSpaceKHR.ColorSpaceSrgbNonlinearKhr, }) { return surfaceFormat; }
			}

			return null;
		}

		[MustUseReturnValue]
		static VkExtent2D ChooseSwapExtent(Vec2<ushort> framebufferSize, VkSurfaceCapabilitiesKHR surfaceCapabilities) =>
				surfaceCapabilities.currentExtent.width != uint.MaxValue ?
						surfaceCapabilities.currentExtent :
						new() {
								width = Math.Clamp(framebufferSize.X, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width),
								height = Math.Clamp(framebufferSize.Y, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height),
						};
	}

	[MustUseReturnValue]
	private static VkImage[] GetSwapChainImages(LogicalGpu logicalGpu, VkSwapchainKHR vkSwapChain) {
		uint swapChainImageCount;
		Vk.GetSwapchainImagesKHR(logicalGpu.VkLogicalDevice, vkSwapChain, &swapChainImageCount, null);

		VkImage[] swapChainImages = new VkImage[swapChainImageCount];
		fixed (VkImage* swapChainImagesPtr = swapChainImages) {
			return Vk.GetSwapchainImagesKHR(logicalGpu.VkLogicalDevice, vkSwapChain, &swapChainImageCount, swapChainImagesPtr) == VkResult.Success ? swapChainImages : throw new Exception(); // TODO exception
		}
	}

	[MustUseReturnValue] // TODO since i'll need this later, should i make a more generic version?
	private static VkImageView[] CreateImageViews(LogicalGpu logicalGpu, VkImage[] images, VkFormat imageFormat, VkImageAspectFlagBits aspectMask) {
		VkImageView[] imageViews = new VkImageView[images.Length];

		fixed (VkImageView* imageViewsPtr = imageViews) {
			// ReSharper disable once LoopCanBeConvertedToQuery // lies
			for (int i = 0; i < images.Length; i++) {
				VkImageViewCreateInfo createInfo = new() {
						image = images[i],
						viewType = VkImageViewType.ImageViewType2D,
						format = imageFormat,
						components = new() {
								r = VkComponentSwizzle.ComponentSwizzleIdentity,
								g = VkComponentSwizzle.ComponentSwizzleIdentity,
								b = VkComponentSwizzle.ComponentSwizzleIdentity,
								a = VkComponentSwizzle.ComponentSwizzleIdentity,
						},
						subresourceRange = new() { aspectMask = aspectMask, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
				};

				if (Vk.CreateImageView(logicalGpu.VkLogicalDevice, &createInfo, null, &imageViewsPtr[i]) != VkResult.Success) { throw new Exception(); } // TODO exception
			}
		}

		return imageViews;
	}
}