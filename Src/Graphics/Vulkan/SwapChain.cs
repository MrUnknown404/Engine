using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class SwapChain {
		public VkSwapchainKHR VkSwapChain { get; private set; }
		public VkImage[] VkImages { get; private set; }
		public VkFormat VkImageFormat { get; private set; }
		public VkExtent2D VkExtent { get; private set; }
		public VkImageView[] VkImageViews { get; private set; }

		public SwapChain(VkSwapchainKHR vkSwapChain, VkImage[] vkImages, VkFormat vkImageFormat, VkExtent2D vkExtent, VkImageView[] vkImageViews) {
			VkSwapChain = vkSwapChain;
			VkImages = vkImages;
			VkImageFormat = vkImageFormat;
			VkExtent = vkExtent;
			VkImageViews = vkImageViews;
		}

		public unsafe void Recreate(VkWindow window) {
			VkDevice vkLogicalDevice = window.LogicalGpu.VkLogicalDevice;

			Vk.DeviceWaitIdle(vkLogicalDevice);
			SwapChain newSwapChain = VkH.CreateSwapChain(window.WindowHandle, window.VkSurface, window.SelectedGpu, vkLogicalDevice, VkPresentModeKHR.PresentModeImmediateKhr, oldSwapChain: VkSwapChain);

			Vk.DestroySwapchainKHR(vkLogicalDevice, VkSwapChain, null);
			foreach (VkImageView vkImageView in VkImageViews) { Vk.DestroyImageView(vkLogicalDevice, vkImageView, null); }

			VkSwapChain = newSwapChain.VkSwapChain;
			VkImages = newSwapChain.VkImages;
			VkImageFormat = newSwapChain.VkImageFormat;
			VkExtent = newSwapChain.VkExtent;
			VkImageViews = newSwapChain.VkImageViews;
		}

		public unsafe void Destroy(VkDevice vkLogicalDevice) {
			Vk.DestroySwapchainKHR(vkLogicalDevice, VkSwapChain, null);
			foreach (VkImageView vkImageView in VkImageViews) { Vk.DestroyImageView(vkLogicalDevice, vkImageView, null); }
		}
	}
}