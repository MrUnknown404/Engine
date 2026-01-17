using System.Diagnostics.CodeAnalysis;
using Engine3.Exceptions;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics.Vulkan {
	public class SwapChain {
		public VkSwapchainKHR VkSwapChain { get; private set; }
		public VkFormat ImageFormat { get; private set; }
		public VkExtent2D Extent { get; private set; }
		public VkImage[] Images { get; private set; }
		public VkImageView[] ImageViews { get; private set; }

		[field: MaybeNull] internal VkWindow Window { private get => field ?? throw new Engine3Exception("Forgot to set window?"); set; }

		private readonly VkPresentModeKHR presentMode;

		private VkDevice LogicalDevice => Window.LogicalGpu.LogicalDevice;

		public SwapChain(VkSwapchainKHR vkSwapChain, VkFormat imageFormat, VkExtent2D extent, VkImage[] images, VkImageView[] imageViews, VkPresentModeKHR presentMode) {
			VkSwapChain = vkSwapChain;
			ImageFormat = imageFormat;
			Extent = extent;
			Images = images;
			ImageViews = imageViews;
			this.presentMode = presentMode;
		}

		public unsafe void Recreate() {
			Vk.DeviceWaitIdle(LogicalDevice);

			Toolkit.Window.GetFramebufferSize(Window.WindowHandle, out Vector2i framebufferSize);
			SwapChain newSwapChain = VkH.CreateSwapChain(Window.SelectedGpu, LogicalDevice, Window.Surface, framebufferSize, presentMode, oldSwapChain: VkSwapChain);

			Vk.DestroySwapchainKHR(LogicalDevice, VkSwapChain, null);
			foreach (VkImageView imageView in ImageViews) { Vk.DestroyImageView(LogicalDevice, imageView, null); }

			VkSwapChain = newSwapChain.VkSwapChain;
			Images = newSwapChain.Images;
			ImageFormat = newSwapChain.ImageFormat;
			Extent = newSwapChain.Extent;
			ImageViews = newSwapChain.ImageViews;
		}

		public unsafe void Destroy() {
			Vk.DestroySwapchainKHR(LogicalDevice, VkSwapChain, null);
			foreach (VkImageView imageView in ImageViews) { Vk.DestroyImageView(LogicalDevice, imageView, null); }
		}
	}
}