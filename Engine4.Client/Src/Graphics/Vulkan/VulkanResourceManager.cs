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
	public GraphicsCommandPool CreateGraphicsCommandPool(VkCommandPoolCreateFlagBits commandPoolCreateFlags) {
		GraphicsCommandPool graphicsCommandPool = new(logicalGpu, commandPoolCreateFlags, physicalGpu.QueueFamilyIndices.GraphicsFamily);
		Add(graphicsCommandPool);
		return graphicsCommandPool;
	}

	[MustUseReturnValue]
	public TransferCommandPool CreateTransferCommandPool(VkCommandPoolCreateFlagBits commandPoolCreateFlags) {
		TransferCommandPool transferCommandPool = new(logicalGpu, commandPoolCreateFlags, physicalGpu.QueueFamilyIndices.TransferFamily);
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
	public Shader CreateShader(string debugName, Assembly assembly, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, VkSpecializationInfo? specializationInfo = null) {
		Shader shader = new(debugName, shaderc, logicalGpu, assembly, fileLocation, shaderLang, shaderType, specializationInfo);
		Add(shader);
		return shader;
	}

	[MustUseReturnValue]
	public VulkanBuffer CreateBuffer(string debugName, ulong size, VkBufferUsageFlagBits2 usageFlags, VkBufferCreateFlagBits createFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) {
		VulkanBuffer buffer = new(debugName, logicalGpu, size, usageFlags, createFlags, memoryPropertyFlags);
		Add(buffer);
		return buffer;
	}

	[MustUseReturnValue]
	public Texture CreateTexture(string debugName, ushort width, ushort height, VkFormat imageFormat, VkImageTiling imageTiling = VkImageTiling.ImageTilingOptimal,
		VkImageUsageFlagBits usageFlags = VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits aspectMask = VkImageAspectFlagBits.ImageAspectColorBit) {
		Texture texture = new(debugName, logicalGpu, width, height, imageFormat, imageTiling, usageFlags, aspectMask);
		Add(texture);
		return texture;
	}

	[Obsolete]
	[MustUseReturnValue]
	public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetInfo[] descriptorSets) {
		DescriptorSetLayout descriptorSetLayout = new(logicalGpu, descriptorSets);
		Add(descriptorSetLayout);
		return descriptorSetLayout;
	}

	[Obsolete]
	[MustUseReturnValue]
	public DescriptorPool CreateDescriptorPool(VkDescriptorType[] descriptorSetTypes, uint count, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags = 0) {
		DescriptorPool descriptorPool = new(logicalGpu, count, descriptorSetTypes, maxFramesInFlight, descriptorPoolCreateFlags);
		Add(descriptorPool);
		return descriptorPool;
	}

	[MustUseReturnValue]
	public Semaphore CreateSemaphore(VkSemaphoreCreateFlags semaphoreCreateFlags) {
		Semaphore semaphore = new(logicalGpu, semaphoreCreateFlags);
		Add(semaphore);
		return semaphore;
	}

	[MustUseReturnValue]
	public Fence CreateFence(VkFenceCreateFlagBits fenceCreateFlags) {
		Fence fence = new(logicalGpu, fenceCreateFlags);
		Add(fence);
		return fence;
	}

	[MustUseReturnValue]
	public Semaphore[] CreateSemaphores(VkSemaphoreCreateFlags semaphoreCreateFlags, uint count) {
		Semaphore[] semaphores = new Semaphore[count];
		for (uint i = 0; i < count; i++) { semaphores[i] = new(logicalGpu, semaphoreCreateFlags); }
		Add(semaphores);
		return semaphores;
	}

	[MustUseReturnValue]
	public Fence[] CreateFences(VkFenceCreateFlagBits fenceCreateFlags, uint count) {
		Fence[] fences = new Fence[count];
		for (uint i = 0; i < count; i++) { fences[i] = new(logicalGpu, fenceCreateFlags); }
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