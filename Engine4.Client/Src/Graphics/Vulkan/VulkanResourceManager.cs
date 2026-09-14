using Engine4.Client.Graphics.Vulkan.Resources;
using Engine4.Client.Rendering;

namespace Engine4.Client.Graphics.Vulkan;

// TODO this should handle the lifetime of all graphics resources. this should handle cleanup.
//  should try to be efficient and reuse objects when necessary. but still allow manual object deletion.

public sealed class VulkanResourceManager {
	private readonly VulkanManager vulkanManager;

	private readonly List<RenderTarget> renderTargets = new(); // TODO allow removal

	// TODO ResourceList class for IVulkanResource?

	internal VulkanResourceManager(VulkanManager vulkanManager) => this.vulkanManager = vulkanManager;

	public VulkanBuffer GetBuffer(ulong size) => throw new NotImplementedException(); // TODO

	public WindowRenderTarget CreateWindowRenderTarget(Window window) {
		WindowRenderTarget renderTarget = new(window, vulkanManager);
		renderTargets.Add(renderTarget);
		return renderTarget;
	}

	public TextureRenderTarget CreateTextureRenderTarget() => throw new NotImplementedException(); // TODO
	public ConsoleRenderTarget CreateConsoleRenderTarget() => throw new NotImplementedException(); // TODO

	public void Cleanup() {
		foreach (RenderTarget renderTarget in renderTargets) { renderTarget.Cleanup(); }
	}
}