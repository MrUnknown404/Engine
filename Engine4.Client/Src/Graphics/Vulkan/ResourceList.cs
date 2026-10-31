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

	private Func<VulkanResource, string> ResourceToNameFunc { get; } = static resource => $"('0x{resource.Handle:X16})";
	private Func<NamedVulkanResource, string> NamedResourceToNameFunc { get; } = static resource => $"('{resource.DebugName}' | 0x{resource.Handle:X16})";

	private readonly List<T> resources = new();
	private readonly Queue<T> resourcesToDestroy = new();

	protected internal void Add(T resource) {
		Logger.Trace($"Tracking {DebugName} {ResourceToName(resource)}");
		resources.Add(resource);
	}

	protected internal void Add(T[] resources) {
		Logger.Trace($"Tracking {resources.Length} {DebugName}s {resources.ElementsAsString(ResourceToName)}");
		this.resources.AddRange(resources);
	}

	protected internal void EnqueueRemove(T resource) {
		bool success = resources.Remove(resource);
		if (success) {
			resourcesToDestroy.Enqueue(resource);
			Logger.Trace($"Enqueued {DebugName} {ResourceToName(resource)} for destruction");
		} else {
			Logger.Warn($"Failed to destroy resource: {DebugName} {ResourceToName(resource)}. Unknown Resource"); //
		}
	}

	/// <summary> Does not call Vk.DeviceWaitIdle() </summary>
	protected internal void RemoveNow(T resource) {
		bool success = resources.Remove(resource);
		if (success) {
			resource.Cleanup();
			Logger.Trace($"Destroyed {DebugName} {ResourceToName(resource)}");
		} else {
			Logger.Warn($"Failed to destroy resource: {DebugName} {ResourceToName(resource)}. Unknown Resource"); //
		}
	}

	public void TryCleanup() {
		if (resourcesToDestroy.Count == 0) { return; }

		List<T> resources = new();

		while (resourcesToDestroy.TryDequeue(out T? resource)) {
			resources.Add(resource);
			resource.Cleanup();
		}

		Logger.Trace($"Destroyed {resources.Count} {DebugName}s {resources.ElementsAsString(ResourceToName)}");
	}

	// TODO add support for bulk cleanup objects
	public void Cleanup() {
		List<T> resources = new();

		foreach (T resource in this.resources) {
			resources.Add(resource);
			resource.Cleanup();
		}

		Logger.Trace($"Destroyed {resources.Count} {typeof(T).Name}s {resources.ElementsAsString(ResourceToName)}");
	}

	private string ResourceToName(T resource) => resource is NamedVulkanResource namedResource ? NamedResourceToNameFunc(namedResource) : ResourceToNameFunc(resource);
}