using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class Surface {
	internal VkSurfaceKHR VkSurface { get; }
	private readonly VulkanInstance vulkanInstance;

	internal Surface(VulkanInstance vulkanInstance, Window window) {
		this.vulkanInstance = vulkanInstance;

		VkSurface = window.CreateSurface(vulkanInstance);
	}

	internal void Cleanup() => Vk.DestroySurfaceKHR(vulkanInstance.VkInstance, VkSurface, null);
}