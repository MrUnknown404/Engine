using OpenTK.Graphics.Vulkan;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public unsafe class Surface {
	public VkSurfaceKHR VkSurface { get; } // TODO private
	private readonly VulkanInstance vulkanInstance;

	internal Surface(VulkanInstance vulkanInstance, Window window) {
		this.vulkanInstance = vulkanInstance;

		GLFW.CreateWindowSurface(new((ulong)vulkanInstance.VkInstance.Handle), window.GlfwWindow, null, out VkHandle handle);
		VkSurface = new(handle.Handle);
	}

	internal void Cleanup() => Vk.DestroySurfaceKHR(vulkanInstance.VkInstance, VkSurface, null);
}