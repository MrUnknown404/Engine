namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract class VulkanResource {
	public string DebugName { get; }
	public abstract ulong Handle { get; }

	protected VulkanResource(string debugName) => DebugName = debugName;

	protected internal abstract void Cleanup(); // TODO bulk clean vs single clean
}