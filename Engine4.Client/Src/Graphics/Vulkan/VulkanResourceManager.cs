using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.IO;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using Semaphore = Engine4.Client.Graphics.Vulkan.Objects.Semaphore;

namespace Engine4.Client.Graphics.Vulkan;

public sealed class VulkanResourceManager {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Vulkan);

	private readonly SurfaceReadyPhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;

	private readonly Dictionary<Type, IResourceList> resourceLists = new();

	internal VulkanResourceManager(SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu) {
		this.physicalGpu = physicalGpu;
		this.logicalGpu = logicalGpu;
	}

	[MustUseReturnValue]
	public GraphicsCommandPool CreateGraphicsCommandPool(string debugName, VkCommandPoolCreateFlagBits commandPoolCreateFlags) {
		GraphicsCommandPool graphicsCommandPool = new(debugName, logicalGpu, commandPoolCreateFlags, physicalGpu.QueueFamilyIndices.GraphicsFamily);
		Add(graphicsCommandPool);
		return graphicsCommandPool;
	}

	[MustUseReturnValue]
	public TransferCommandPool CreateTransferCommandPool(string debugName, VkCommandPoolCreateFlagBits commandPoolCreateFlags) {
		TransferCommandPool transferCommandPool = new(debugName, logicalGpu, commandPoolCreateFlags, physicalGpu.QueueFamilyIndices.TransferFamily);
		Add(transferCommandPool);
		return transferCommandPool;
	}

	[MustUseReturnValue]
	public VulkanBuffer CreateBuffer(string debugName, ulong size) {
		VulkanBuffer vulkanBuffer = new(debugName, size);
		Add(vulkanBuffer);
		return vulkanBuffer;
	}

	[MustUseReturnValue]
	public Semaphore CreateSemaphore(string debugName, VkSemaphoreCreateFlags semaphoreCreateFlags) {
		Semaphore semaphore = new(debugName, logicalGpu, semaphoreCreateFlags);
		Add(semaphore);
		return semaphore;
	}

	[MustUseReturnValue]
	public Fence CreateFence(string debugName, VkFenceCreateFlagBits fenceCreateFlags) {
		Fence fence = new(debugName, logicalGpu, fenceCreateFlags);
		Add(fence);
		return fence;
	}

	[MustUseReturnValue]
	public Semaphore[] CreateSemaphores(string debugName, VkSemaphoreCreateFlags semaphoreCreateFlags, uint count) {
		Semaphore[] semaphores = new Semaphore[count];
		for (uint i = 0; i < count; i++) { semaphores[i] = CreateSemaphore($"{debugName} [{i}]", semaphoreCreateFlags); }
		return semaphores;
	}

	[MustUseReturnValue]
	public Fence[] CreateFences(string debugName, uint count, VkFenceCreateFlagBits fenceCreateFlags) {
		Fence[] fences = new Fence[count];
		for (uint i = 0; i < count; i++) { fences[i] = CreateFence($"{debugName} [{i}]", fenceCreateFlags); }
		return fences;
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
		foreach (IResourceList resourceList in resourceLists.Values) {
			Logger.Trace($"- Cleaning up resource: {resourceList.DebugName}");
			resourceList.Cleanup();
		}
	}
}