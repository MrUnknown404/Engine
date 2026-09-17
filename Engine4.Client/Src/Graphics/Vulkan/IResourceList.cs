namespace Engine4.Client.Graphics.Vulkan;

public interface IResourceList {
	public string DebugName { get; }
	public void Cleanup();
}