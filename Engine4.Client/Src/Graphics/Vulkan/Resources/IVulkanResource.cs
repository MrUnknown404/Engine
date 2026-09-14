namespace Engine4.Client.Graphics.Vulkan.Resources;

public interface IVulkanResource {
	public string DebugName { get; }
	public ulong Handle { get; }

	// TODO in debug mode the engine should automatically print what resources are made and deleted
}