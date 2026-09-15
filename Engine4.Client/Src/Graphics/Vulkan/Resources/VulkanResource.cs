namespace Engine4.Client.Graphics.Vulkan.Resources;

// TODO in debug mode the engine should automatically print what resources are made and deleted
public abstract class VulkanResource {
	public string DebugName { get; }
	protected abstract ulong Handle { get; }

	protected VulkanResource(string debugName) => DebugName = debugName;

	protected internal abstract void Cleanup();
}