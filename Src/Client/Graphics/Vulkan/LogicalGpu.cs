using System.Reflection;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using Engine3.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using StbiSharp;

namespace Engine3.Client.Graphics.Vulkan {
	public unsafe class LogicalGpu : IDestroyable {
		public VkDevice LogicalDevice { get; }
		public VkQueue GraphicsQueue { get; }
		public VkQueue PresentQueue { get; }
		public VkQueue TransferQueue { get; }

		public bool WasDestroyed { get; private set; }
		private readonly PhysicalGpu physicalGpu;

		internal LogicalGpu(PhysicalGpu physicalGpu, VkDevice logicalDevice, VkQueue graphicsQueue, VkQueue presentQueue, VkQueue transferQueue) {
			this.physicalGpu = physicalGpu;
			LogicalDevice = logicalDevice;
			GraphicsQueue = graphicsQueue;
			PresentQueue = presentQueue;
			TransferQueue = transferQueue;
		}

		[MustUseReturnValue]
		public VkShaderObject CreateShader(string debugName, string fileName, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) => new(debugName, LogicalDevice, fileName, shaderLang, shaderType, assembly);

		[MustUseReturnValue] public GraphicsPipeline CreateGraphicsPipeline(GraphicsPipeline.Settings settings) => new(physicalGpu, LogicalDevice, settings);

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
		public VkBufferObject CreateBuffer(string debugName, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) {
			VkBufferCreateInfo bufferCreateInfo = new() { size = bufferSize, usage = bufferUsageFlags, sharingMode = VkSharingMode.SharingModeExclusive, };
			VkBuffer buffer;
			VkH.CheckIfSuccess(Vk.CreateBuffer(LogicalDevice, &bufferCreateInfo, null, &buffer), VulkanException.Reason.CreateBuffer);

			VkDeviceMemory bufferMemory = CreateDeviceMemory(buffer, memoryPropertyFlags);
			BindBufferMemory(LogicalDevice, buffer, bufferMemory);

			return new(debugName, this, buffer, bufferMemory, bufferSize);
		}

		[MustUseReturnValue]
		public UniformBuffers CreateUniformBuffers(string debugName, VkRenderer renderer, ulong bufferSize) {
			VkBufferObject[] buffers = new VkBufferObject[renderer.MaxFramesInFlight];
			void*[] buffersMapped = new void*[renderer.MaxFramesInFlight];

			for (int i = 0; i < renderer.MaxFramesInFlight; i++) {
				VkBufferObject buffer = CreateBuffer($"Test Uniform Buffer[{i}]", VkBufferUsageFlagBits.BufferUsageUniformBufferBit,
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
		public VkImageObject CreateImage(string debugName, uint width, uint height, VkFormat imageFormat) =>
				CreateImage(debugName, width, height, imageFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits.ImageAspectColorBit);

		[MustUseReturnValue]
		public VkImageObject CreateImage(string debugName, uint width, uint height, VkFormat imageFormat, VkImageTiling imageTiling, VkImageUsageFlagBits usageFlags, VkImageAspectFlagBits aspectMask) {
			VkImage image = CreateImage(LogicalDevice, imageFormat, imageTiling, usageFlags, width, height);
			VkDeviceMemory imageMemory = CreateDeviceMemory(image, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);
			BindImageMemory(LogicalDevice, image, imageMemory);
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

		[MustUseReturnValue]
		public VkImageObject CreateImageAndCopyUsingStaging(string debugName, string fileLocation, string fileExtension, uint width, uint height, byte texChannels, VkFormat imageFormat, VkCommandPool transferCommandPool,
			Assembly assembly) {
			using (StbiImage stbiImage = AssetH.LoadImage(fileLocation, fileExtension, texChannels, assembly)) {
				VkImageObject image = CreateImage(debugName, width, height, imageFormat, VkImageTiling.ImageTilingOptimal, VkImageUsageFlagBits.ImageUsageSampledBit, VkImageAspectFlagBits.ImageAspectColorBit);
				image.Copy(transferCommandPool, TransferQueue, stbiImage, 4);
				return image;
			}
		}

		[MustUseReturnValue]
		public VkImageObject CreateImageAndCopyUsingStaging(string debugName, string fileLocation, string fileExtension, uint width, uint height, byte texChannels, VkFormat imageFormat, VkImageTiling imageTiling,
			VkImageUsageFlagBits usageFlags, VkImageAspectFlagBits aspectMask, VkCommandPool transferCommandPool, Assembly assembly) {
			using (StbiImage stbiImage = AssetH.LoadImage(fileLocation, fileExtension, texChannels, assembly)) {
				VkImageObject image = CreateImage(debugName, width, height, imageFormat, imageTiling, usageFlags, aspectMask);
				image.Copy(transferCommandPool, TransferQueue, stbiImage, 4);
				return image;
			}
		}

		[MustUseReturnValue] public DepthImage CreateDepthImage(string debugName, VkCommandPool transferCommandPool, VkExtent2D extent) => new(debugName, physicalGpu, this, transferCommandPool, TransferQueue, extent);

		[MustUseReturnValue]
		public TransferCommandBufferObject CreateTransferCommandBuffer(VkCommandPool commandPool, VkQueue queue, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) =>
				new(LogicalDevice, commandPool, queue, level);

		[MustUseReturnValue]
		public VkDescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetInfo[] descriptorSets) {
			VkDescriptorSetLayoutBinding[] bindings = descriptorSets.Select(static info => new VkDescriptorSetLayoutBinding {
					binding = info.BindingLocation, descriptorType = info.DescriptorType, stageFlags = info.StageFlags, descriptorCount = 1,
			}).ToArray();

			fixed (VkDescriptorSetLayoutBinding* bindingsPtr = bindings) {
				VkDescriptorSetLayoutCreateInfo layoutCreateInfo = new() { bindingCount = (uint)bindings.Length, pBindings = bindingsPtr, };
				VkDescriptorSetLayout descriptorSetLayout;
				VkH.CheckIfSuccess(Vk.CreateDescriptorSetLayout(LogicalDevice, &layoutCreateInfo, null, &descriptorSetLayout), VulkanException.Reason.CreateDescriptorSetLayout);
				return descriptorSetLayout;
			}
		}

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Vk.DeviceWaitIdle(LogicalDevice);
			Vk.DestroyDevice(LogicalDevice, null);

			WasDestroyed = true;
		}

		private static void BindBufferMemory(VkDevice logicalDevice, VkBuffer buffer, VkDeviceMemory deviceMemory) {
			VkBindBufferMemoryInfo bindBufferMemoryInfo = new() { buffer = buffer, memory = deviceMemory, };
			VkH.CheckIfSuccess(Vk.BindBufferMemory2(logicalDevice, 1, &bindBufferMemoryInfo), VulkanException.Reason.BindBufferMemory);
		}

		private static void BindImageMemory(VkDevice logicalDevice, VkImage image, VkDeviceMemory deviceMemory) {
			VkBindImageMemoryInfo bindImageMemoryInfo = new() { image = image, memory = deviceMemory, };
			VkH.CheckIfSuccess(Vk.BindImageMemory2(logicalDevice, 1, &bindImageMemoryInfo), VulkanException.Reason.BindImageMemory);
		}
	}
}