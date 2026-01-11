using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class SwapChain {
		public VkSwapchainKHR VkSwapChain { get; }
		public VkImage[] VkImages { get; }
		public VkFormat VkImageFormat { get; }
		public VkExtent2D VkExtent { get; }
		public VkImageView[] VkImageViews { get; }

		public SwapChain(VkSwapchainKHR vkSwapChain, VkImage[] vkImages, VkFormat vkImageFormat, VkExtent2D vkExtent, VkImageView[] vkImageViews) {
			VkSwapChain = vkSwapChain;
			VkImages = vkImages;
			VkImageFormat = vkImageFormat;
			VkExtent = vkExtent;
			VkImageViews = vkImageViews;
		}

		public unsafe void Destroy(VkDevice vkLogicalDevice) {
			Vk.DestroySwapchainKHR(vkLogicalDevice, VkSwapChain, null);
			foreach (VkImageView vkImageView in VkImageViews) { Vk.DestroyImageView(vkLogicalDevice, vkImageView, null); }
		}
	}
}