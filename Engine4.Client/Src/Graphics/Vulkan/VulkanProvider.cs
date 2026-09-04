using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Graphics.Vulkan;

// TODO this should handle the lifetime of all graphics resources. this should handle cleanup.
//  should try to be efficient and reuse objects when necessary. but still allow manual object deletion.

public sealed class VulkanProvider : IGraphicsProvider {
	internal VulkanProvider() { }

	public VulkanBuffer GetBuffer(ulong size) => throw new NotImplementedException(); // TODO

	public void Cleanup() { } // TODO cleanup
}