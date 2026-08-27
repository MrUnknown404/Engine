namespace Engine4.Graphics.Vulkan;

// TODO this should handle the lifetime of all graphics resources. this should handle cleanup.
//  should try to be efficient and reuse objects when necessary. but still allow manual object deletion.

public class VulkanGraphicsProvider : IGraphicsApiProvider {
	internal VulkanGraphicsProvider() { }

	public GraphicsBuffer GetBuffer(ulong size) => throw new NotImplementedException(); // TODO
}