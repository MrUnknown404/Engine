using System.Reflection;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	[PublicAPI]
	public unsafe class LogicalGpu : IDestroyable {
		public VkDevice LogicalDevice { get; }
		public VkQueue GraphicsQueue { get; }
		public VkQueue PresentQueue { get; }
		public VkQueue TransferQueue { get; }

		public bool WasDestroyed { get; private set; }
		private readonly SurfaceCapablePhysicalGpu physicalGpu;

		internal LogicalGpu(SurfaceCapablePhysicalGpu physicalGpu, VkDevice logicalDevice, VkQueue graphicsQueue, VkQueue presentQueue, VkQueue transferQueue) {
			this.physicalGpu = physicalGpu;
			LogicalDevice = logicalDevice;
			GraphicsQueue = graphicsQueue;
			PresentQueue = presentQueue;
			TransferQueue = transferQueue;
		}

		[MustUseReturnValue]
		public VulkanShader CreateShader(string debugName, string fileName, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) => new(debugName, LogicalDevice, fileName, shaderLang, shaderType, assembly);

		[MustUseReturnValue] internal GraphicsPipeline CreateGraphicsPipeline(GraphicsPipeline.Settings settings) => new(physicalGpu, LogicalDevice, settings);

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
			// The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
			// among many different objects by using the offset parameters that we've seen in many functions."
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

		[MustUseReturnValue]
		public VulkanBuffer CreateBuffer(string debugName, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) {
			VkBufferCreateInfo bufferCreateInfo = new() { size = bufferSize, usage = bufferUsageFlags, sharingMode = VkSharingMode.SharingModeExclusive, };
			VkBuffer buffer;
			VkH.CheckIfSuccess(Vk.CreateBuffer(LogicalDevice, &bufferCreateInfo, null, &buffer), VulkanException.Reason.CreateBuffer);

			VkDeviceMemory bufferMemory = CreateDeviceMemory(buffer, memoryPropertyFlags);
			BindBufferMemory(buffer, bufferMemory);

			return new(debugName, this, buffer, bufferMemory, bufferSize);
		}

		[MustUseReturnValue]
		public UniformBuffers CreateUniformBuffers(string debugName, VulkanRenderer renderer, ulong bufferSize) {
			VulkanBuffer[] buffers = new VulkanBuffer[renderer.MaxFramesInFlight];
			void*[] buffersMapped = new void*[renderer.MaxFramesInFlight];

			for (int i = 0; i < renderer.MaxFramesInFlight; i++) {
				VulkanBuffer buffer = CreateBuffer($"{debugName}[{i}]", VkBufferUsageFlagBits.BufferUsageUniformBufferBit,
					VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize);

				buffers[i] = buffer;
				buffersMapped[i] = buffer.MapMemory(bufferSize);
			}

			return new(debugName, bufferSize, renderer, buffers, buffersMapped);
		}

		[MustUseReturnValue]
		public TextureSampler CreateSampler(TextureSampler.Settings settings) {
			VkSamplerCreateInfo samplerCreateInfo = new() {
					minFilter = settings.MinFilter,
					magFilter = settings.MagFilter,
					addressModeU = settings.AddressMode.U,
					addressModeV = settings.AddressMode.V,
					addressModeW = settings.AddressMode.W,
					anisotropyEnable =
							(int)(settings.AnisotropyEnable && (Engine3.GameInstance.GraphicsBackend as VulkanGraphicsBackend ?? throw new Engine3Exception("Wrong graphics api is in use")).AllowEnableAnisotropy ?
									Vk.True :
									Vk.False),
					maxAnisotropy = settings.MaxAnisotropy,
					borderColor = settings.BorderColor,
					unnormalizedCoordinates = (int)(settings.NormalizedCoordinates ? Vk.False : Vk.True),
					compareEnable = (int)Vk.False,
					compareOp = VkCompareOp.CompareOpAlways,
					mipmapMode = settings.MipmapMode,
					mipLodBias = settings.MipLodBias,
					minLod = settings.MinLod,
					maxLod = settings.MaxLod,
			};

			VkSampler textureSampler;
			VkH.CheckIfSuccess(Vk.CreateSampler(LogicalDevice, &samplerCreateInfo, null, &textureSampler), VulkanException.Reason.CreateTextureSampler);

			return new(LogicalDevice, textureSampler);
		}

		[MustUseReturnValue]
		public VulkanImage CreateImage(string debugName, uint width, uint height, VkFormat imageFormat, VkImageTiling imageTiling, VkImageUsageFlagBits usageFlags, VkImageAspectFlagBits aspectMask) {
			VkImage image = CreateImage(LogicalDevice, imageFormat, imageTiling, usageFlags, width, height);
			VkDeviceMemory imageMemory = CreateDeviceMemory(image, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
			BindImageMemory(image, imageMemory);
			VkImageView imageView = CreateImageView(LogicalDevice, image, imageFormat, aspectMask);

			return new(debugName, physicalGpu, this, image, imageMemory, imageView, imageFormat);

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

		[MustUseReturnValue] public DepthImage CreateDepthImage(string debugName, VkCommandPool transferCommandPool, VkExtent2D extent) => new(debugName, physicalGpu, this, transferCommandPool, TransferQueue, extent);

		[MustUseReturnValue]
		public VkCommandPool CreateCommandPool(VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			VkH.CheckIfSuccess(Vk.CreateCommandPool(LogicalDevice, &commandPoolCreateInfo, null, &commandPool), VulkanException.Reason.CreateCommandPool);
			return commandPool;
		}

		[MustUseReturnValue]
		public GraphicsCommandBuffer[] CreateCommandBuffers(VkCommandPool commandPool, uint count, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = count, };
			VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];
			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				VkH.CheckIfSuccess(Vk.AllocateCommandBuffers(LogicalDevice, &commandBufferAllocateInfo, commandBuffersPtr), VulkanException.Reason.AllocateCommandBuffers);
			}

			GraphicsCommandBuffer[] buffers = new GraphicsCommandBuffer[count];
			for (int i = 0; i < commandBuffers.Length; i++) { buffers[i] = new(LogicalDevice, commandPool, commandBuffers[i]); }

			return buffers;
		}

		[MustUseReturnValue]
		public GraphicsCommandBuffer CreateGraphicsCommandBuffer(VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) => new(LogicalDevice, commandPool, level);

		[MustUseReturnValue]
		public TransferCommandBuffer CreateTransferCommandBuffer(VkCommandPool commandPool, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) => new(LogicalDevice, commandPool, level);

		[MustUseReturnValue] public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetInfo[] descriptorSets) => new(LogicalDevice, descriptorSets);

		[MustUseReturnValue] public DescriptorPool CreateDescriptorPool(uint poolCount, VkDescriptorType[] descriptorTypes, byte maxFramesInFlight) => new(LogicalDevice, poolCount, descriptorTypes, maxFramesInFlight);

		[MustUseReturnValue]
		public VkSemaphore[] CreateSemaphores(uint count) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore[] semaphores = new VkSemaphore[count];

			fixed (VkSemaphore* semaphoresPtr = semaphores) {
				for (uint i = 0; i < count; i++) { VkH.CheckIfSuccess(Vk.CreateSemaphore(LogicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]), VulkanException.Reason.CreateSemaphore); }
			}

			return semaphores;
		}

		[MustUseReturnValue]
		public VkSemaphore CreateSemaphore() {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore semaphore;
			VkH.CheckIfSuccess(Vk.CreateSemaphore(LogicalDevice, &semaphoreCreateInfo, null, &semaphore), VulkanException.Reason.CreateSemaphore);
			return semaphore;
		}

		[MustUseReturnValue]
		public VkFence[] CreateFences(uint count) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence[] fences = new VkFence[count];

			fixed (VkFence* fencesPtr = fences) {
				for (uint i = 0; i < count; i++) { VkH.CheckIfSuccess(Vk.CreateFence(LogicalDevice, &fenceCreateInfo, null, &fencesPtr[i]), VulkanException.Reason.CreateFence); }
			}

			return fences;
		}

		[MustUseReturnValue]
		public VkFence CreateFence() {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence fence;
			VkH.CheckIfSuccess(Vk.CreateFence(LogicalDevice, &fenceCreateInfo, null, &fence), VulkanException.Reason.CreateFence);
			return fence;
		}

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Vk.DeviceWaitIdle(LogicalDevice);
			Vk.DestroyDevice(LogicalDevice, null);

			WasDestroyed = true;
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