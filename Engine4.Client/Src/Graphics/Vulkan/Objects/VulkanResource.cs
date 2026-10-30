namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract class VulkanResource {
	public abstract ulong Handle { get; }

	protected internal abstract void Cleanup(); // TODO bulk clean vs single clean
}