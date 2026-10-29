using System.Reflection;
using Engine4.Client.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using Silk.NET.Shaderc;
using Semaphore = Engine4.Client.Graphics.Vulkan.Objects.Semaphore;

namespace Engine4.Client.Graphics.Vulkan;

public sealed class VulkanResourceManager {
	private readonly SurfaceReadyPhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;
	private readonly Shaderc shaderc;

	private readonly Dictionary<Type, IResourceList> resourceLists = new();

	internal VulkanResourceManager(SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, Shaderc shaderc) {
		this.physicalGpu = physicalGpu;
		this.logicalGpu = logicalGpu;
		this.shaderc = shaderc;
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
	public GraphicsPipeline CreateGraphicsPipeline(GraphicsPipeline.Settings settings) {
		GraphicsPipeline graphicsPipeline = new(physicalGpu, logicalGpu, settings);
		Add(graphicsPipeline);
		return graphicsPipeline;
	}

	[MustUseReturnValue]
	public VulkanShader CreateShader(string debugName, Assembly assembly, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, VkSpecializationInfo? specializationInfo = null) {
		VulkanShader shader = new(debugName, shaderc, logicalGpu, assembly, fileLocation, shaderLang, shaderType, specializationInfo);
		Add(shader);
		return shader;
	}

	[MustUseReturnValue]
	public VulkanBuffer CreateBuffer(string debugName, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits createFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) {
		VulkanBuffer vulkanBuffer = new(debugName, logicalGpu, size, usageFlags, createFlags, memoryPropertyFlags);
		Add(vulkanBuffer);
		return vulkanBuffer;
	}

	[MustUseReturnValue]
	public VulkanTexture CreateTexture(string debugName) => throw new NotImplementedException(); // TODO

	[MustUseReturnValue]
	public TextureSampler CreateTextureSampler(string debugName) => throw new NotImplementedException(); // TODO

	[Obsolete]
	[MustUseReturnValue]
	public DescriptorSetLayout CreateDescriptorSetLayout(string debugName, DescriptorSetInfo[] descriptorSets) {
		DescriptorSetLayout descriptorSetLayout = new(debugName, logicalGpu, descriptorSets);
		Add(descriptorSetLayout);
		return descriptorSetLayout;
	}

	[Obsolete]
	[MustUseReturnValue]
	public DescriptorPool CreateDescriptorPool(string debugName, VkDescriptorType[] descriptorSetTypes, uint count, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags = 0) {
		DescriptorPool descriptorPool = new(debugName, logicalGpu, count, descriptorSetTypes, maxFramesInFlight, descriptorPoolCreateFlags);
		Add(descriptorPool);
		return descriptorPool;
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
		for (uint i = 0; i < count; i++) { semaphores[i] = new($"{debugName} [{i}]", logicalGpu, semaphoreCreateFlags); }
		Add(semaphores);
		return semaphores;
	}

	[MustUseReturnValue]
	public Fence[] CreateFences(string debugName, uint count, VkFenceCreateFlagBits fenceCreateFlags) {
		Fence[] fences = new Fence[count];
		for (uint i = 0; i < count; i++) { fences[i] = new($"{debugName} [{i}]", logicalGpu, fenceCreateFlags); }
		Add(fences);
		return fences;
	}

	private void Add<T>(T resource) where T : VulkanResource => GetResourceList<T>().Add(resource);
	private void Add<T>(T[] resource) where T : VulkanResource => GetResourceList<T>().Add(resource);

	public void EnqueueDestroy<T>(T resource) where T : VulkanResource => GetResourceList<T>().EnqueueRemove(resource);

	/// <summary> Does not call Vk.DeviceWaitIdle() </summary>
	public void DestroyNow<T>(T resource) where T : VulkanResource => GetResourceList<T>().RemoveNow(resource);

	[MustUseReturnValue]
	private ResourceList<T> GetResourceList<T>() where T : VulkanResource {
		if (resourceLists.TryGetValue(typeof(T), out IResourceList? list)) { return list as ResourceList<T> ?? throw new InvalidCastException(); }
		list = new ResourceList<T>();
		resourceLists.Add(typeof(T), list);
		return list as ResourceList<T> ?? throw new InvalidCastException();
	}

	internal void TryCleanup() {
		bool hasWaited = false;

		foreach (IResourceList resourceList in resourceLists.Values.Where(static resourceList => resourceList.HasResourcesToCleanUp)) {
			if (!hasWaited) {
				Vk.DeviceWaitIdle(logicalGpu.VkLogicalDevice);
				hasWaited = true;
			}

			resourceList.TryCleanup();
		}
	}

	internal void Cleanup() {
		foreach (IResourceList resourceList in resourceLists.Values) { resourceList.Cleanup(); }
	}
}