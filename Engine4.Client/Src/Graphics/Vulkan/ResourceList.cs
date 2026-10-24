using System.Diagnostics.CodeAnalysis;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.IO;
using NLog;
using USharpLibs.Common.Utils;

namespace Engine4.Client.Graphics.Vulkan;

public class ResourceList<T> : IResourceList where T : VulkanResource {
	[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Vulkan);

	public string DebugName { get; } = typeof(T).Name;
	public bool IsEmpty => resources.Count == 0;
	public bool HasResourcesToCleanUp => resourcesToDestroy.Count != 0;

	private readonly List<T> resources = new();
	private readonly Queue<T> resourcesToDestroy = new();

	internal void Add(T resource) {
		Logger.Trace($"Tracking {typeof(T).Name} ('{resource.DebugName}' | 0x{resource.Handle:X16})");
		resources.Add(resource);
	}

	internal void Add(T[] resources) {
		Logger.Trace($"Tracking {resources.Length} {typeof(T).Name}s {resources.ElementsAsString(static r => $"('{r.DebugName}' | 0x{r.Handle:X16})")}");
		this.resources.AddRange(resources);
	}

	internal void EnqueueRemove(T resource) {
		bool success = resources.Remove(resource);
		if (success) {
			resourcesToDestroy.Enqueue(resource);
			Logger.Trace($"Enqueued {typeof(T).Name} ('{resource.DebugName}' | 0x{resource.Handle:X16}) for destruction");
		} else {
			Logger.Warn($"Failed to destroy resource: {typeof(T).Name} ('{resource.DebugName}' | 0x{resource.Handle:X16}). Unknown Resource"); //
		}
	}

	internal void RemoveNow(T resource) {
		bool success = resources.Remove(resource);
		if (success) {
			resource.Cleanup();
			Logger.Trace($"Destroyed {typeof(T).Name} ('{resource.DebugName}' | 0x{resource.Handle:X16})");
		} else {
			Logger.Warn($"Failed to destroy resource: {typeof(T).Name} ('{resource.DebugName}' | 0x{resource.Handle:X16}). Unknown Resource"); //
		}
	}

	public void TryCleanup() {
		if (resourcesToDestroy.Count == 0) { return; }

		List<T> resources = new();

		while (resourcesToDestroy.TryDequeue(out T? resource)) {
			resources.Add(resource);
			resource.Cleanup();
		}

		Logger.Trace($"Destroyed {resources.Count} {typeof(T).Name}s {resources.ElementsAsString(static r => $"('{r.DebugName}' | 0x{r.Handle:X16})")}");
	}

	// TODO add support for bulk cleanup objects
	public void Cleanup() {
		List<T> resources = new();

		foreach (T resource in this.resources) {
			resources.Add(resource);
			resource.Cleanup();
		}

		Logger.Trace($"Destroyed {resources.Count} {typeof(T).Name}s {resources.ElementsAsString(static r => $"('{r.DebugName}' | 0x{r.Handle:X16})")}");
	}
}