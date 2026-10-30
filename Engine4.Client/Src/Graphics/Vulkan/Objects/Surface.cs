using Engine4.Client.Windowing;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class Surface : VulkanResource {
	public VkSurfaceKHR VkSurface { get; }
	public override ulong Handle => VkSurface.Handle;

	private readonly VulkanInstance vulkanInstance;

	internal Surface(VulkanInstance vulkanInstance, Window window) {
		this.vulkanInstance = vulkanInstance;

		VkSurface = window.CreateSurface(vulkanInstance);
	}

	protected internal override void Cleanup() => Vk.DestroySurfaceKHR(vulkanInstance.VkInstance, VkSurface, null);
}