using Engine4.Client.Graphics.Vulkan.Objects;

namespace Engine4.Client.Graphics.Vulkan;

// TODO debug logging
// TODO way of manually marking resources as ready for cleanup

public class ResourceList<T> : IResourceList where T : VulkanResource {
	public string DebugName { get; } = typeof(T).Name;

	private readonly List<T> resources = new();

	internal void Add(T resource) => resources.Add(resource);

	internal bool Remove(T resource) {
		bool success = resources.Remove(resource);
		if (success) { resource.Cleanup(); }
		return success;
	}

	public void Cleanup() {
		// TODO add support for bulk cleanup objects

		foreach (T resource in resources) { resource.Cleanup(); }
	}
}