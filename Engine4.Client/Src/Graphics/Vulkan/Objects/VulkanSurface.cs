using OpenTK.Graphics.Vulkan;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GlfwWindow = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class VulkanSurface {
	private readonly VulkanInstance vulkanInstance;
	private readonly VkSurfaceKHR vkSurface;

	internal VulkanSurface(VulkanInstance vulkanInstance, GlfwWindow* glfwWindow) {
		this.vulkanInstance = vulkanInstance;

		GLFW.CreateWindowSurface(new((ulong)vulkanInstance.VkInstance.Handle), glfwWindow, null, out VkHandle handle);
		vkSurface = new(handle.Handle);
	}

	internal void Cleanup() => Vk.DestroySurfaceKHR(vulkanInstance.VkInstance, vkSurface, null);
}