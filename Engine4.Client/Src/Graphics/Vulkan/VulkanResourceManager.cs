using Engine4.Client.Graphics.Vulkan.Resources;

namespace Engine4.Client.Graphics.Vulkan;

// TODO this should handle the lifetime of all graphics resources. this should handle cleanup.
//  should try to be efficient and reuse objects when necessary. but still allow manual object deletion.

public sealed class VulkanResourceManager {
	// TODO ResourceList class for IVulkanResource?

	public VulkanBuffer GetBuffer(ulong size) => throw new NotImplementedException(); // TODO

	public void Cleanup() {
		//
	}
}