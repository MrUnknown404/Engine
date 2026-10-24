namespace Engine4.Client.Graphics.Vulkan;

public interface IResourceList {
	public string DebugName { get; }
	public bool IsEmpty { get; }
	public bool HasResourcesToCleanUp { get; }

	public void TryCleanup();
	public void Cleanup();
}