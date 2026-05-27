using System.Reflection;
using Engine3.Client.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Utility.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Client.Graphics.Vulkan;

public unsafe class VulkanResourceProvider : IGraphicsResourceProvider {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private readonly ResourceManager<GraphicsPipeline> graphicsPipelineManager = new();
	private readonly ResourceManager<CommandPool> commandPoolManager = new();
	private readonly ResourceManager<CommandBuffer> commandBufferManager = new();
	private readonly ResourceManager<DescriptorSetLayout> descriptorSetLayoutManager = new();
	private readonly ResourceManager<DescriptorPool> descriptorPoolManager = new();
	private readonly ResourceManager<VulkanShader> shaderManager = new();
	private readonly ResourceManager<VulkanBuffer> bufferManager = new();
	private readonly ResourceManager<DescriptorBuffers> descriptorsBufferManager = new();
	private readonly ResourceManager<DescriptorBuffer> descriptorBufferManager = new();
	private readonly ResourceManager<VulkanImage> imageManager = new();
	private readonly ResourceManager<TextureSampler> samplerManager = new();

	private readonly SurfaceCapablePhysicalGpu physicalGpu;
	private readonly LogicalGpu logicalGpu;

	internal VulkanResourceProvider(SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu) {
		this.physicalGpu = physicalGpu;
		this.logicalGpu = logicalGpu;
	}

	[MustUseReturnValue]
	public GraphicsPipeline CreateGraphicsPipeline(GraphicsPipeline.Settings settings) {
		GraphicsPipeline graphicsPipeline = new(physicalGpu, logicalGpu, settings);
		graphicsPipelineManager.Add(graphicsPipeline);
		return graphicsPipeline;
	}

	[MustUseReturnValue]
	public TransferCommandPool CreateTransferCommandPool(VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
		TransferCommandPool commandPool = new(logicalGpu, this, commandPoolCreateFlags, queueFamilyIndex);
		commandPoolManager.Add(commandPool);
		return commandPool;
	}

	[MustUseReturnValue]
	public GraphicsCommandPool CreateGraphicsCommandPool(VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
		GraphicsCommandPool commandPool = new(logicalGpu, this, commandPoolCreateFlags, queueFamilyIndex);
		commandPoolManager.Add(commandPool);
		return commandPool;
	}

	[MustUseReturnValue]
	public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetInfo[] descriptorSets) {
		DescriptorSetLayout descriptorSetLayout = new(logicalGpu, descriptorSets);
		descriptorSetLayoutManager.Add(descriptorSetLayout);
		return descriptorSetLayout;
	}

	[MustUseReturnValue]
	public DescriptorPool CreateDescriptorPool(VkDescriptorType[] descriptorSetTypes, uint count, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags = 0) {
		DescriptorPool descriptorPool = new(logicalGpu, count, descriptorSetTypes, maxFramesInFlight, descriptorPoolCreateFlags);
		descriptorPoolManager.Add(descriptorPool);
		return descriptorPool;
	}

	[MustUseReturnValue]
	public TextureSampler CreateSampler(TextureSampler.Settings settings) {
		TextureSampler sampler = new(logicalGpu, settings);
		samplerManager.Add(sampler);
		return sampler;
	}

	[MustUseReturnValue]
	public VulkanShader CreateShader(string debugName, string fileName, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly, VkSpecializationInfo? specializationInfo = null) {
		VulkanShader shader = new(debugName, logicalGpu, fileName, shaderLang, shaderType, specializationInfo, assembly);
		shaderManager.Add(shader);
		return shader;
	}

	[MustUseReturnValue]
	public VulkanBuffer CreateBuffer(string debugName, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) {
		VkBufferCreateInfo bufferCreateInfo = new() { size = bufferSize, usage = bufferUsageFlags, sharingMode = VkSharingMode.SharingModeExclusive, };
		VkBuffer buffer;
		VkH.CheckIfSuccess(Vk.CreateBuffer(logicalGpu.LogicalDevice, &bufferCreateInfo, null, &buffer), VulkanException.Reason.CreateBuffer);

		VkDeviceMemory bufferMemory = logicalGpu.CreateDeviceMemory(buffer, memoryPropertyFlags);
		logicalGpu.BindBufferMemory(buffer, bufferMemory);

		VulkanBuffer vulkanBuffer = new(debugName, logicalGpu, buffer, bufferMemory, bufferUsageFlags, memoryPropertyFlags, bufferSize);
		bufferManager.Add(vulkanBuffer);
		return vulkanBuffer;
	}

	[MustUseReturnValue]
	public DescriptorBuffers CreateDescriptorBuffers(string debugName, ulong bufferSize, byte maxFramesInFlight, VkDescriptorType descriptorType, VkBufferUsageFlagBits bufferUsageFlags) {
		DescriptorBuffers descriptorBuffer = new(debugName, this, bufferSize, maxFramesInFlight, bufferUsageFlags, descriptorType);
		descriptorsBufferManager.Add(descriptorBuffer);
		return descriptorBuffer;
	}

	[MustUseReturnValue]
	public DescriptorBuffer CreateDescriptorBuffer(string debugName, ulong bufferSize, VkDescriptorType descriptorType, VkBufferUsageFlagBits bufferUsageFlags) {
		DescriptorBuffer descriptorBuffer = new(debugName, this, bufferSize, bufferUsageFlags, descriptorType);
		descriptorBufferManager.Add(descriptorBuffer);
		return descriptorBuffer;
	}

	[MustUseReturnValue]
	public VulkanImage CreateImage(string debugName, uint width, uint height, VkFormat imageFormat, VkImageTiling imageTiling = VkImageTiling.ImageTilingOptimal,
		VkImageUsageFlagBits usageFlags = VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, VkComponentMapping? componentMapping = null) {
		VkImage image = CreateImage(logicalGpu.LogicalDevice, imageFormat, imageTiling, usageFlags, width, height);
		VkDeviceMemory imageMemory = logicalGpu.CreateDeviceMemory(image, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
		logicalGpu.BindImageMemory(image, imageMemory);

		VkImageView imageView = CreateImageView(logicalGpu.LogicalDevice, image, imageFormat, aspectMask,
			componentMapping ??
			new() { r = VkComponentSwizzle.ComponentSwizzleIdentity, g = VkComponentSwizzle.ComponentSwizzleIdentity, b = VkComponentSwizzle.ComponentSwizzleIdentity, a = VkComponentSwizzle.ComponentSwizzleIdentity, });

		VulkanImage vulkanImage = new(debugName, logicalGpu, image, imageMemory, imageView, imageFormat);
		imageManager.Add(vulkanImage);
		return vulkanImage;

		[MustUseReturnValue]
		static VkImage CreateImage(VkDevice logicalDevice, VkFormat imageFormat, VkImageTiling tiling, VkImageUsageFlagBits usage, uint width, uint height) {
			VkImageCreateInfo imageCreateInfo = new() {
					imageType = VkImageType.ImageType2d,
					format = imageFormat,
					tiling = tiling,
					initialLayout = VkImageLayout.ImageLayoutUndefined,
					usage = usage | VkImageUsageFlagBits.ImageUsageTransferDstBit,
					sharingMode = VkSharingMode.SharingModeExclusive,
					samples = VkSampleCountFlagBits.SampleCount1Bit,
					flags = 0,
					extent = new() { width = width, height = height, depth = 1, },
					mipLevels = 1,
					arrayLayers = 1,
			};

			VkImage tempImage;
			VkH.CheckIfSuccess(Vk.CreateImage(logicalDevice, &imageCreateInfo, null, &tempImage), VulkanException.Reason.CreateImage);
			return tempImage;
		}

		[MustUseReturnValue]
		static VkImageView CreateImageView(VkDevice logicalDevice, VkImage image, VkFormat imageFormat, VkImageAspectFlagBits aspectMask, VkComponentMapping componentMapping) {
			VkImageViewCreateInfo createInfo = new() {
					image = image,
					viewType = VkImageViewType.ImageViewType2d,
					format = imageFormat,
					components = componentMapping,
					subresourceRange = new() { aspectMask = aspectMask, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			VkImageView imageView;
			VkH.CheckIfSuccess(Vk.CreateImageView(logicalDevice, &createInfo, null, &imageView), VulkanException.Reason.CreateImageView);
			return imageView;
		}
	}

	[Obsolete]
	internal void AddCommandBuffer(CommandBuffer commandBuffer) => commandBufferManager.Add(commandBuffer);

	public void EnqueueDestroy(GraphicsPipeline graphicsPipeline) {
		Logger.Trace($"Requesting to destroy {nameof(GraphicsPipeline)} ({graphicsPipeline.Pipeline.Handle:X16})");
		graphicsPipelineManager.EnqueueDestroy(graphicsPipeline);
	}

	public void EnqueueDestroy(CommandPool commandPool) {
		Logger.Trace($"Requesting to destroy {nameof(CommandPool)} ({commandPool.VkCommandPool.Handle:X16})");
		commandPoolManager.EnqueueDestroy(commandPool);
	}

	public void EnqueueDestroy(CommandBuffer commandBuffer) {
		Logger.Trace($"Requesting to destroy {nameof(CommandBuffer)} ({commandBuffer.VkCommandBuffer.Handle:X16})");
		commandBufferManager.EnqueueDestroy(commandBuffer);
	}

	public void EnqueueDestroy(DescriptorBuffers descriptorBuffers) {
		Logger.Trace($"Requesting to destroy {nameof(DescriptorBuffers)} ({descriptorBuffers.GetBuffer(0).Buffer.Handle:X16})");
		descriptorsBufferManager.EnqueueDestroy(descriptorBuffers);
	}

	public void EnqueueDestroy(DescriptorBuffer descriptorBuffer) {
		Logger.Trace($"Requesting to destroy {nameof(DescriptorBuffer)} ({descriptorBuffer.Buffer.Buffer.Handle:X16})");
		descriptorBufferManager.EnqueueDestroy(descriptorBuffer);
	}

	public void EnqueueDestroy(DescriptorSetLayout descriptorSetLayout) {
		Logger.Trace($"Requesting to destroy {nameof(DescriptorSetLayout)} ({descriptorSetLayout.VkDescriptorSetLayout.Handle:X16})");
		descriptorSetLayoutManager.EnqueueDestroy(descriptorSetLayout);
	}

	public void EnqueueDestroy(DescriptorPool descriptorPool) {
		Logger.Trace($"Requesting to destroy {nameof(DescriptorPool)} ({descriptorPool.VkDescriptorPool.Handle:X16})");
		descriptorPoolManager.EnqueueDestroy(descriptorPool);
	}

	public void EnqueueDestroy(TextureSampler sampler) {
		Logger.Trace($"Requesting to destroy {nameof(TextureSampler)} ({sampler.Sampler.Handle:X16})");
		samplerManager.EnqueueDestroy(sampler);
	}

	public void EnqueueDestroy(VulkanShader shader) {
		Logger.Trace($"Requesting to destroy {nameof(VulkanShader)} ({shader.ShaderModule.Handle:X16})");
		shaderManager.EnqueueDestroy(shader);
	}

	public void EnqueueDestroy(VulkanBuffer buffer) {
		Logger.Trace($"Requesting to destroy {nameof(VulkanBuffer)} ({buffer.Buffer.Handle:X16})");
		bufferManager.EnqueueDestroy(buffer);
	}

	public void EnqueueDestroy(VulkanImage image) {
		Logger.Trace($"Requesting to destroy {nameof(VulkanImage)} ({image.Image.Handle:X16})");
		imageManager.EnqueueDestroy(image);
	}

	public void TryCleanupResources() {
		Vk.DeviceWaitIdle(logicalGpu.LogicalDevice); // FIXME bad. only call if needed

		graphicsPipelineManager.TryCleanup();
		commandBufferManager.TryCleanup();
		commandPoolManager.TryCleanup();
		descriptorSetLayoutManager.TryCleanup();
		descriptorPoolManager.TryCleanup();
		shaderManager.TryCleanup();
		bufferManager.TryCleanup();
		descriptorsBufferManager.TryCleanup();
		descriptorBufferManager.TryCleanup();
		samplerManager.TryCleanup();
		imageManager.TryCleanup();
	}

	internal void Cleanup() {
		graphicsPipelineManager.CleanupAll();
		commandBufferManager.CleanupAll();
		commandPoolManager.CleanupAll();
		descriptorSetLayoutManager.CleanupAll();
		descriptorPoolManager.CleanupAll();
		shaderManager.CleanupAll();
		bufferManager.CleanupAll();
		descriptorsBufferManager.CleanupAll();
		descriptorBufferManager.CleanupAll();
		samplerManager.CleanupAll();
		imageManager.CleanupAll();
	}
}