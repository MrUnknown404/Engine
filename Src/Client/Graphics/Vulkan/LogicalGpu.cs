using System.Reflection;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	[PublicAPI]
	public sealed unsafe class LogicalGpu : GraphicsResource<LogicalGpu, ulong>, IGraphicsResourceProvider { // TODO cleanup all these vulkan classes
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkDevice LogicalDevice { get; }
		public VkQueue GraphicsQueue { get; }
		public VkQueue PresentQueue { get; }
		public VkQueue TransferQueue { get; }

		protected override ulong Handle => LogicalDevice.Handle;

		private readonly ResourceManager<GraphicsPipeline> graphicsPipelineManager = new();
		private readonly ResourceManager<CommandPool> commandPoolManager = new();
		private readonly ResourceManager<CommandBuffer> commandBufferManager = new();
		private readonly ResourceManager<DescriptorSetLayout> descriptorSetLayoutManager = new();
		private readonly ResourceManager<DescriptorPool> descriptorPoolManager = new();
		private readonly ResourceManager<VulkanShader> shaderManager = new();
		private readonly ResourceManager<VulkanBuffer> bufferManager = new();
		private readonly ResourceManager<DescriptorBuffers> descriptorBufferManager = new();
		private readonly ResourceManager<VulkanImage> imageManager = new();
		private readonly ResourceManager<TextureSampler> samplerManager = new();

		private readonly SurfaceCapablePhysicalGpu physicalGpu;

		internal LogicalGpu(SurfaceCapablePhysicalGpu physicalGpu, VkDevice logicalDevice, VkQueue graphicsQueue, VkQueue presentQueue, VkQueue transferQueue) {
			this.physicalGpu = physicalGpu;
			LogicalDevice = logicalDevice;
			GraphicsQueue = graphicsQueue;
			PresentQueue = presentQueue;
			TransferQueue = transferQueue;

			PrintCreate();
		}

		[MustUseReturnValue]
		public GraphicsPipeline CreateGraphicsPipeline(GraphicsPipeline.Settings settings) {
			GraphicsPipeline graphicsPipeline = new(physicalGpu, LogicalDevice, settings);
			graphicsPipelineManager.Add(graphicsPipeline);
			return graphicsPipeline;
		}

		[MustUseReturnValue]
		public TransferCommandPool CreateTransferCommandPool(VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			TransferCommandPool commandPool = new(this, commandPoolCreateFlags, queueFamilyIndex);
			commandPoolManager.Add(commandPool);
			return commandPool;
		}

		[MustUseReturnValue]
		public GraphicsCommandPool CreateGraphicsCommandPool(VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			GraphicsCommandPool commandPool = new(this, commandPoolCreateFlags, queueFamilyIndex);
			commandPoolManager.Add(commandPool);
			return commandPool;
		}

		[MustUseReturnValue]
		public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetInfo[] descriptorSets) {
			DescriptorSetLayout descriptorSetLayout = new(LogicalDevice, descriptorSets);
			descriptorSetLayoutManager.Add(descriptorSetLayout);
			return descriptorSetLayout;
		}

		[MustUseReturnValue]
		public DescriptorPool CreateDescriptorPool(VkDescriptorType[] descriptorSetTypes, uint count, byte maxFramesInFlight, VkDescriptorPoolCreateFlagBits descriptorPoolCreateFlags = 0) {
			DescriptorPool descriptorPool = new(LogicalDevice, count, descriptorSetTypes, maxFramesInFlight, descriptorPoolCreateFlags);
			descriptorPoolManager.Add(descriptorPool);
			return descriptorPool;
		}

		[MustUseReturnValue]
		public TextureSampler CreateSampler(TextureSampler.Settings settings) {
			TextureSampler sampler = new(LogicalDevice, settings);
			samplerManager.Add(sampler);
			return sampler;
		}

		[MustUseReturnValue]
		public VulkanShader CreateShader(string debugName, string fileName, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly, VkSpecializationInfo? specializationInfo = null) { // TODO settings
			VulkanShader shader = new(debugName, LogicalDevice, fileName, shaderLang, shaderType, specializationInfo, assembly);
			shaderManager.Add(shader);
			return shader;
		}

		[MustUseReturnValue]
		public VulkanBuffer CreateBuffer(string debugName, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) {
			VkBufferCreateInfo bufferCreateInfo = new() { size = bufferSize, usage = bufferUsageFlags, sharingMode = VkSharingMode.SharingModeExclusive, };
			VkBuffer buffer;
			VkH.CheckIfSuccess(Vk.CreateBuffer(LogicalDevice, &bufferCreateInfo, null, &buffer), VulkanException.Reason.CreateBuffer);

			VkDeviceMemory bufferMemory = CreateDeviceMemory(buffer, memoryPropertyFlags);
			BindBufferMemory(buffer, bufferMemory);

			VulkanBuffer vulkanBuffer = new(debugName, this, buffer, bufferMemory, bufferSize);
			bufferManager.Add(vulkanBuffer);
			return vulkanBuffer;
		}

		[MustUseReturnValue]
		public DescriptorBuffers CreateDescriptorBuffers(string debugName, ulong bufferSize, byte maxFramesInFlight, VkDescriptorType descriptorType, VkBufferUsageFlagBits bufferUsageFlags) {
			DescriptorBuffers descriptorBuffer = new(debugName, this, bufferSize, maxFramesInFlight, bufferUsageFlags, descriptorType);
			descriptorBufferManager.Add(descriptorBuffer);
			return descriptorBuffer;
		}

		[MustUseReturnValue]
		public VulkanImage CreateImage(string debugName, uint width, uint height, VkFormat imageFormat, VkImageTiling imageTiling = VkImageTiling.ImageTilingOptimal,
			VkImageUsageFlagBits usageFlags = VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits aspectMask = VkImageAspectFlagBits.ImageAspectColorBit) {
			VkImage image = CreateImage(LogicalDevice, imageFormat, imageTiling, usageFlags, width, height);
			VkDeviceMemory imageMemory = CreateDeviceMemory(image, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
			BindImageMemory(image, imageMemory);
			VkImageView imageView = CreateImageView(LogicalDevice, image, imageFormat, aspectMask);

			VulkanImage vulkanImage = new(debugName, this, image, imageMemory, imageView, imageFormat);
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
			static VkImageView CreateImageView(VkDevice logicalDevice, VkImage image, VkFormat imageFormat, VkImageAspectFlagBits aspectMask) {
				VkImageViewCreateInfo createInfo = new() {
						image = image,
						viewType = VkImageViewType.ImageViewType2d,
						format = imageFormat,
						components = new() {
								r = VkComponentSwizzle.ComponentSwizzleIdentity, g = VkComponentSwizzle.ComponentSwizzleIdentity, b = VkComponentSwizzle.ComponentSwizzleIdentity, a = VkComponentSwizzle.ComponentSwizzleIdentity,
						},
						subresourceRange = new() { aspectMask = aspectMask, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
				};

				VkImageView imageView;
				VkH.CheckIfSuccess(Vk.CreateImageView(logicalDevice, &createInfo, null, &imageView), VulkanException.Reason.CreateImageView);
				return imageView;
			}
		}

		[MustUseReturnValue]
		public VkDeviceMemory CreateDeviceMemory(VkBuffer buffer, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkBufferMemoryRequirementsInfo2 bufferMemoryRequirementsInfo2 = new() { buffer = buffer, };
			VkMemoryRequirements2 memoryRequirements2 = new();
			Vk.GetBufferMemoryRequirements2(LogicalDevice, &bufferMemoryRequirementsInfo2, &memoryRequirements2);
			return CreateDeviceMemory(memoryRequirements2.memoryRequirements, memoryPropertyFlags);
		}

		[MustUseReturnValue]
		public VkDeviceMemory CreateDeviceMemory(VkImage image, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkImageMemoryRequirementsInfo2 imageMemoryRequirementsInfo2 = new() { image = image, };
			VkMemoryRequirements2 memoryRequirements2 = new();
			Vk.GetImageMemoryRequirements2(LogicalDevice, &imageMemoryRequirementsInfo2, &memoryRequirements2);
			return CreateDeviceMemory(memoryRequirements2.memoryRequirements, memoryPropertyFlags);
		}

		[MustUseReturnValue]
		public VkDeviceMemory CreateDeviceMemory(VkMemoryRequirements memoryRequirements, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkMemoryAllocateInfo memoryAllocateInfo = new() {
					allocationSize = memoryRequirements.size, memoryTypeIndex = FindMemoryType(physicalGpu.PhysicalDeviceMemoryProperties2.memoryProperties, memoryRequirements.memoryTypeBits, memoryPropertyFlags),
			};

			// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
			//  The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
			//  among many different objects by using the offset parameters that we've seen in many functions."
			VkDeviceMemory deviceMemory;
			VkH.CheckIfSuccess(Vk.AllocateMemory(LogicalDevice, &memoryAllocateInfo, null, &deviceMemory), VulkanException.Reason.AllocateMemory);
			return deviceMemory;

			[MustUseReturnValue]
			static uint FindMemoryType(VkPhysicalDeviceMemoryProperties memoryProperties, uint typeFilter, VkMemoryPropertyFlagBits memoryPropertyFlag) {
				for (int i = 0; i < memoryProperties.memoryTypeCount; i++) {
					if ((typeFilter & (1 << i)) != 0 && (memoryProperties.memoryTypes[i].propertyFlags & memoryPropertyFlag) == memoryPropertyFlag) { return (uint)i; }
				}

				throw new Engine3VulkanException("Failed to find suitable memory type");
			}
		}

		[MustUseReturnValue] public DepthImage CreateDepthImage(TransferCommandPool transferCommandPool, VkExtent2D extent) => new(physicalGpu, this, transferCommandPool, TransferQueue, extent);

		[MustUseReturnValue]
		public VkSemaphore[] CreateSemaphores(uint count) { // TODO auto resource
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore[] semaphores = new VkSemaphore[count];

			fixed (VkSemaphore* semaphoresPtr = semaphores) {
				for (uint i = 0; i < count; i++) { VkH.CheckIfSuccess(Vk.CreateSemaphore(LogicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]), VulkanException.Reason.CreateSemaphore); }
			}

			return semaphores;
		}

		[MustUseReturnValue]
		public VkSemaphore CreateSemaphore() { // TODO auto resource
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore semaphore;
			VkH.CheckIfSuccess(Vk.CreateSemaphore(LogicalDevice, &semaphoreCreateInfo, null, &semaphore), VulkanException.Reason.CreateSemaphore);
			return semaphore;
		}

		[MustUseReturnValue]
		public VkFence[] CreateFences(uint count) { // TODO auto resource
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence[] fences = new VkFence[count];

			fixed (VkFence* fencesPtr = fences) {
				for (uint i = 0; i < count; i++) { VkH.CheckIfSuccess(Vk.CreateFence(LogicalDevice, &fenceCreateInfo, null, &fencesPtr[i]), VulkanException.Reason.CreateFence); }
			}

			return fences;
		}

		[MustUseReturnValue]
		public VkFence CreateFence() { // TODO auto resource
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence fence;
			VkH.CheckIfSuccess(Vk.CreateFence(LogicalDevice, &fenceCreateInfo, null, &fence), VulkanException.Reason.CreateFence);
			return fence;
		}

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
			Logger.Trace($"Requesting to destroy {nameof(DescriptorBuffers)} ({descriptorBuffers.GetBuffer(0).Handle:X16})");
			descriptorBufferManager.EnqueueDestroy(descriptorBuffers);
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
			Vk.DeviceWaitIdle(LogicalDevice); // FIXME bad. only call if needed

			graphicsPipelineManager.TryCleanup();
			commandBufferManager.TryCleanup();
			commandPoolManager.TryCleanup();
			descriptorSetLayoutManager.TryCleanup();
			descriptorPoolManager.TryCleanup();
			shaderManager.TryCleanup();
			bufferManager.TryCleanup();
			descriptorBufferManager.TryCleanup();
			samplerManager.TryCleanup();
			imageManager.TryCleanup();
		}

		protected override void Cleanup() {
			Vk.DeviceWaitIdle(LogicalDevice);

			graphicsPipelineManager.CleanupAll();
			commandBufferManager.CleanupAll();
			commandPoolManager.CleanupAll();
			descriptorSetLayoutManager.CleanupAll();
			descriptorPoolManager.CleanupAll();
			shaderManager.CleanupAll();
			bufferManager.CleanupAll();
			descriptorBufferManager.CleanupAll();
			samplerManager.CleanupAll();
			imageManager.CleanupAll();

			Vk.DestroyDevice(LogicalDevice, null);
		}

		private void BindBufferMemory(VkBuffer buffer, VkDeviceMemory deviceMemory) {
			VkBindBufferMemoryInfo bindBufferMemoryInfo = new() { buffer = buffer, memory = deviceMemory, };
			VkH.CheckIfSuccess(Vk.BindBufferMemory2(LogicalDevice, 1, &bindBufferMemoryInfo), VulkanException.Reason.BindBufferMemory);
		}

		private void BindImageMemory(VkImage image, VkDeviceMemory deviceMemory) {
			VkBindImageMemoryInfo bindImageMemoryInfo = new() { image = image, memory = deviceMemory, };
			VkH.CheckIfSuccess(Vk.BindImageMemory2(LogicalDevice, 1, &bindImageMemoryInfo), VulkanException.Reason.BindImageMemory);
		}
	}
}