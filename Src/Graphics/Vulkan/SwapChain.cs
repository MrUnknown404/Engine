using System.Diagnostics.CodeAnalysis;
using Engine3.Exceptions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics.Vulkan {
	public class SwapChain {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSwapchainKHR VkSwapChain { get; private set; }
		public VkFormat ImageFormat { get; private set; }
		public VkExtent2D Extent { get; private set; }
		public VkImage[] Images { get; private set; }
		public VkImageView[] ImageViews { get; private set; }

		[field: MaybeNull] internal VkWindow Window { private get => field ?? throw new Engine3Exception("Forgot to set window?"); set; }

		private readonly VkDevice logicalDevice;
		private readonly VkPresentModeKHR presentMode;
		private bool wasDestroyed;

		public SwapChain(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, QueueFamilyIndices queueFamilyIndices, WindowHandle windowHandle, VkSurfaceKHR surface, VkPresentModeKHR presentMode) {
			Toolkit.Window.GetFramebufferSize(windowHandle, out Vector2i framebufferSize);
			VkH.CreateSwapChain(physicalDevice, logicalDevice, surface, queueFamilyIndices, framebufferSize, out VkSwapchainKHR vkSwapChain, out VkExtent2D swapChainExtent, out VkFormat swapChainImageFormat, presentMode);

			this.logicalDevice = logicalDevice;
			VkSwapChain = vkSwapChain;
			ImageFormat = swapChainImageFormat;
			Extent = swapChainExtent;
			Images = VkH.GetSwapChainImages(logicalDevice, vkSwapChain);
			ImageViews = VkH.CreateImageViews(logicalDevice, Images, ImageFormat);
			this.presentMode = presentMode;
		}

		public void Recreate() {
			Vk.DeviceWaitIdle(logicalDevice);

			Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i framebufferSize);
			VkH.CreateSwapChain(Window.SelectedGpu.PhysicalDevice, logicalDevice, Window.Surface, Window.SelectedGpu.QueueFamilyIndices, framebufferSize, out VkSwapchainKHR vkSwapChain, out VkExtent2D swapChainExtent,
				out VkFormat swapChainImageFormat, presentMode, oldSwapChain: VkSwapChain);

			Logger.Debug("Recreated swap chain");

			Destroy();

			VkSwapChain = vkSwapChain;
			ImageFormat = swapChainImageFormat;
			Extent = swapChainExtent;
			Images = VkH.GetSwapChainImages(logicalDevice, vkSwapChain);
			ImageViews = VkH.CreateImageViews(logicalDevice, Images, swapChainImageFormat);
			wasDestroyed = false;
		}

		public unsafe void Destroy() {
			if (wasDestroyed) {
				Logger.Warn($"{nameof(SwapChain)} was already destroyed");
				return;
			}

			Vk.DestroySwapchainKHR(logicalDevice, VkSwapChain, null);
			foreach (VkImageView imageView in ImageViews) { Vk.DestroyImageView(logicalDevice, imageView, null); }

			wasDestroyed = true;
		}
	}
}