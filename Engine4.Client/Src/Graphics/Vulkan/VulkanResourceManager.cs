using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Graphics.Vulkan.Resources;
using Engine4.IO;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public sealed class VulkanResourceManager {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Vulkan);

	private readonly BoundPhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;

	private readonly Dictionary<Type, IResourceList> resourceLists = new();

	internal VulkanResourceManager(BoundPhysicalGpu physicalGpu, LogicalGpu logicalGpu) {
		this.physicalGpu = physicalGpu;
		this.logicalGpu = logicalGpu;
	}

	public GraphicsCommandPool CreateGraphicsCommandPool(string debugName, VkCommandPoolCreateFlagBits commandPoolCreateFlags) {
		GraphicsCommandPool graphicsCommandPool = new(debugName, logicalGpu, commandPoolCreateFlags, physicalGpu.QueueFamilyIndices.GraphicsFamily);
		Add(graphicsCommandPool);
		return graphicsCommandPool;
	}

	public TransferCommandPool CreateTransferCommandPool(string debugName, VkCommandPoolCreateFlagBits commandPoolCreateFlags) {
		TransferCommandPool transferCommandPool = new(debugName, logicalGpu, commandPoolCreateFlags, physicalGpu.QueueFamilyIndices.TransferFamily);
		Add(transferCommandPool);
		return transferCommandPool;
	}

	public VulkanBuffer CreateBuffer(string debugName, ulong size) {
		VulkanBuffer vulkanBuffer = new(debugName, size);
		Add(vulkanBuffer);
		return vulkanBuffer;
	}

	private void Add<T>(T resource) where T : VulkanResource => GetResourceList<T>().Add(resource);

	public void Destroy<T>(T resource) where T : VulkanResource {
		if (!GetResourceList<T>().Remove(resource)) { Logger.Warn($"Failed to destroy resource: {resource.DebugName}. Unknown Resource"); }
	}

	private ResourceList<T> GetResourceList<T>() where T : VulkanResource {
		if (resourceLists.TryGetValue(typeof(T), out IResourceList? list)) { return list as ResourceList<T> ?? throw new InvalidCastException(); }
		list = new ResourceList<T>();
		resourceLists.Add(typeof(T), list);
		return list as ResourceList<T> ?? throw new InvalidCastException();
	}

	internal void Cleanup() {
		foreach (IResourceList resourceList in resourceLists.Values) { resourceList.Cleanup(); }
	}
}