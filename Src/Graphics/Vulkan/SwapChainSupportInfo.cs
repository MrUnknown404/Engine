using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public readonly record struct SwapChainSupportInfo {
		public required VkSurfaceCapabilities2KHR VkCapabilities { get; init; }
		public required VkSurfaceFormat2KHR[] VkSurfaceFormat { get; init; }
		public required VkPresentModeKHR[] VkPresentMode { get; init; }

		[SetsRequiredMembers]
		public SwapChainSupportInfo(VkSurfaceCapabilities2KHR vkCapabilities, VkSurfaceFormat2KHR[] vkSurfaceFormat, VkPresentModeKHR[] vkPresentMode) {
			VkCapabilities = vkCapabilities;
			VkSurfaceFormat = vkSurfaceFormat;
			VkPresentMode = vkPresentMode;
		}
	}
}