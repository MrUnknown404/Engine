using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public readonly record struct SwapChainSupportInfo {
		public required VkSurfaceCapabilities2KHR VkCapabilities { get; init; }
		public required VkSurfaceFormat2KHR[] VkSurfaceFormats { get; init; }
		public required VkPresentModeKHR[] VkPresentModes { get; init; }

		[SetsRequiredMembers]
		public SwapChainSupportInfo(VkSurfaceCapabilities2KHR vkCapabilities, VkSurfaceFormat2KHR[] vkSurfaceFormats, VkPresentModeKHR[] vkPresentModes) {
			VkCapabilities = vkCapabilities;
			VkSurfaceFormats = vkSurfaceFormats;
			VkPresentModes = vkPresentModes;
		}
	}
}