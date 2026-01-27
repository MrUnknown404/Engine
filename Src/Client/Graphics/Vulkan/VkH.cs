using Engine3.Exceptions;
using Engine3.Utility.Versions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.Vulkan {
	public static unsafe partial class VkH {
		/// <summary>
		/// Helper method for extracting version information out of a packed uint following Vulkan's Version Specification.<br/><br/>
		/// The variant is packed into bits 31-29.<br/>
		/// The major version is packed into bits 28-22.<br/>
		/// The minor version number is packed into bits 21-12.<br/>
		/// The patch version number is packed into bits 11-0.<br/>
		/// </summary>
		/// <param name="version"> Packed integer containing the other 'out' parameters </param>
		/// <param name="variant"> 3-bit integer </param>
		/// <param name="major"> 7-bit integer </param>
		/// <param name="minor"> 10-bit integer </param>
		/// <param name="patch"> 12-bit integer </param>
		/// <seealso href="https://docs.vulkan.org/spec/latest/chapters/extensions.html#extendingvulkan-coreversions">Relevant Vulkan Specification</seealso>
		[Pure]
		public static void GetApiVersion(uint version, out byte variant, out byte major, out ushort minor, out ushort patch) {
			variant = (byte)(version >> 29);
			major = (byte)((version >> 22) & 0x7FU);
			minor = (ushort)((version >> 12) & 0x3FFU);
			patch = (ushort)(version & 0xFFFU);
		}

		[MustUseReturnValue]
		public static Version4<ushort> GetApiVersion(uint version) {
			GetApiVersion(version, out byte variant, out byte major, out ushort minor, out ushort patch);
			return new(variant, major, minor) { Hotfix = patch, };
		}

		public static void CheckIfSuccess(VkResult result, VulkanException.Reason reason, params object?[] args) {
			if (result != VkResult.Success) { throw new VulkanException(reason, result, args); }
		}

		[MustUseReturnValue]
		public static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) {
			CheckIfSuccess(Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR surface), VulkanException.Reason.CreateSurface);
			return surface;
		}

		[MustUseReturnValue]
		public static VkSemaphore[] CreateSemaphores(VkDevice logicalDevice, uint count) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore[] semaphores = new VkSemaphore[count];

			fixed (VkSemaphore* semaphoresPtr = semaphores) {
				for (uint i = 0; i < count; i++) { CheckIfSuccess(Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]), VulkanException.Reason.CreateSemaphore); }
			}

			return semaphores;
		}

		[MustUseReturnValue]
		public static VkSemaphore CreateSemaphore(VkDevice logicalDevice) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore semaphore;
			CheckIfSuccess(Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphore), VulkanException.Reason.CreateSemaphore);
			return semaphore;
		}

		[MustUseReturnValue]
		public static VkFence[] CreateFences(VkDevice logicalDevice, uint count) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence[] fences = new VkFence[count];

			fixed (VkFence* fencesPtr = fences) {
				for (uint i = 0; i < count; i++) { CheckIfSuccess(Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fencesPtr[i]), VulkanException.Reason.CreateFence); }
			}

			return fences;
		}

		[MustUseReturnValue]
		public static VkFence CreateFence(VkDevice logicalDevice) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence fence;
			CheckIfSuccess(Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fence), VulkanException.Reason.CreateFence);
			return fence;
		}

		[MustUseReturnValue]
		public static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferCreateInfo bufferCreateInfo) {
			VkBuffer buffer;
			CheckIfSuccess(Vk.CreateBuffer(logicalDevice, &bufferCreateInfo, null, &buffer), VulkanException.Reason.CreateBuffer);
			return buffer;
		}

		[MustUseReturnValue]
		public static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsage, ulong size, uint[]? queueFamilyIndices = null) {
			if (queueFamilyIndices == null) { return CreateBuffer(logicalDevice, new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeExclusive, }); }

			fixed (uint* queueFamilyIndicesPtr = queueFamilyIndices) {
				return CreateBuffer(logicalDevice,
					new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeConcurrent, queueFamilyIndexCount = (uint)queueFamilyIndices.Length, pQueueFamilyIndices = queueFamilyIndicesPtr, });
			}
		}

		[MustUseReturnValue]
		public static VkDeviceMemory CreateDeviceMemory(VkPhysicalDeviceMemoryProperties2 memoryProperties, VkDevice logicalDevice, VkBuffer buffer, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkBufferMemoryRequirementsInfo2 bufferMemoryRequirementsInfo2 = new() { buffer = buffer, };
			VkMemoryRequirements2 memoryRequirements2 = new();
			Vk.GetBufferMemoryRequirements2(logicalDevice, &bufferMemoryRequirementsInfo2, &memoryRequirements2);
			return CreateDeviceMemory(memoryProperties, logicalDevice, memoryRequirements2.memoryRequirements, memoryPropertyFlags);
		}

		[MustUseReturnValue]
		public static VkDeviceMemory CreateDeviceMemory(VkPhysicalDeviceMemoryProperties2 memoryProperties, VkDevice logicalDevice, VkImage image, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkImageMemoryRequirementsInfo2 imageMemoryRequirementsInfo2 = new() { image = image, };
			VkMemoryRequirements2 memoryRequirements2 = new();
			Vk.GetImageMemoryRequirements2(logicalDevice, &imageMemoryRequirementsInfo2, &memoryRequirements2);
			return CreateDeviceMemory(memoryProperties, logicalDevice, memoryRequirements2.memoryRequirements, memoryPropertyFlags);
		}

		[MustUseReturnValue]
		private static VkDeviceMemory CreateDeviceMemory(VkPhysicalDeviceMemoryProperties2 memoryProperties, VkDevice logicalDevice, VkMemoryRequirements memoryRequirements, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkMemoryAllocateInfo memoryAllocateInfo = new() {
					allocationSize = memoryRequirements.size, memoryTypeIndex = FindMemoryType(memoryProperties.memoryProperties, memoryRequirements.memoryTypeBits, memoryPropertyFlags),
			};

			VkDeviceMemory deviceMemory;

			// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
			// The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
			// among many different objects by using the offset parameters that we've seen in many functions."
			CheckIfSuccess(Vk.AllocateMemory(logicalDevice, &memoryAllocateInfo, null, &deviceMemory), VulkanException.Reason.AllocateMemory);
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
		public static void* MapMemory(VkDevice logicalDevice, VkDeviceMemory deviceMemory, ulong bufferSize, ulong offset) {
			VkMemoryMapInfo memoryMapInfo = new() { memory = deviceMemory, size = bufferSize, offset = offset, };
			void* dataPtr;
			Vk.MapMemory2(logicalDevice, &memoryMapInfo, &dataPtr);
			return dataPtr;
		}

		public static void CopyMemory(void* srcDataPtr, void* dstDataPtr, ulong dstSize, ulong sourceBytesToCopy) => Buffer.MemoryCopy(srcDataPtr, dstDataPtr, dstSize, sourceBytesToCopy);

		public static void UnmapMemory(VkDevice logicalDevice, VkDeviceMemory deviceMemory) {
			VkMemoryUnmapInfo memoryUnmapInfo = new() { memory = deviceMemory, };
			Vk.UnmapMemory2(logicalDevice, &memoryUnmapInfo);
		}

		public static void MapAndCopyMemory<T>(VkDevice logicalDevice, VkDeviceMemory deviceMemory, ReadOnlySpan<T> inData, ulong offset) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * inData.Length);
			fixed (T* inDataPtr = inData) {
				void* dataPtr = MapMemory(logicalDevice, deviceMemory, bufferSize, offset);
				CopyMemory(inDataPtr, dataPtr, bufferSize, bufferSize);
				UnmapMemory(logicalDevice, deviceMemory);
			}
		}

		public static void BindBufferMemory(VkDevice logicalDevice, VkBuffer buffer, VkDeviceMemory deviceMemory) {
			VkBindBufferMemoryInfo bindBufferMemoryInfo = new() { buffer = buffer, memory = deviceMemory, };
			CheckIfSuccess(Vk.BindBufferMemory2(logicalDevice, 1, &bindBufferMemoryInfo), VulkanException.Reason.BindBufferMemory);
		}

		public static void BindImageMemory(VkDevice logicalDevice, VkImage image, VkDeviceMemory deviceMemory) {
			VkBindImageMemoryInfo bindImageMemoryInfo = new() { image = image, memory = deviceMemory, };
			CheckIfSuccess(Vk.BindImageMemory2(logicalDevice, 1, &bindImageMemoryInfo), VulkanException.Reason.BindImageMemory);
		}

		[MustUseReturnValue]
		public static VkQueue GetDeviceQueue(VkDevice logicalDevice, uint queueFamilyIndex) {
			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
			VkQueue queue;
			Vk.GetDeviceQueue2(logicalDevice, &deviceQueueInfo2, &queue);
			return queue;
		}

		public static void SubmitQueues(VkQueue queue, VkSubmitInfo[] submitInfos, VkFence? fence) {
			fixed (VkSubmitInfo* submitInfosPtr = submitInfos) { CheckIfSuccess(Vk.QueueSubmit(queue, (uint)submitInfos.Length, submitInfosPtr, fence ?? VkFence.Zero), VulkanException.Reason.QueueSubmit); }
		}

		public static void SubmitQueue(VkQueue queue, VkSubmitInfo submitInfo, VkFence? fence) =>
				CheckIfSuccess(Vk.QueueSubmit(queue, 1, &submitInfo, fence ?? VkFence.Zero), VulkanException.Reason.QueueSubmit); // TODO device lost?

		[MustUseReturnValue]
		public static VkImageView CreateImageView(VkDevice logicalDevice, VkImage image, VkFormat imageFormat) {
			VkImageViewCreateInfo createInfo = new() {
					image = image,
					viewType = VkImageViewType.ImageViewType2d,
					format = imageFormat,
					components = new() {
							r = VkComponentSwizzle.ComponentSwizzleIdentity, g = VkComponentSwizzle.ComponentSwizzleIdentity, b = VkComponentSwizzle.ComponentSwizzleIdentity, a = VkComponentSwizzle.ComponentSwizzleIdentity,
					},
					subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
			};

			VkImageView imageView;
			CheckIfSuccess(Vk.CreateImageView(logicalDevice, &createInfo, null, &imageView), VulkanException.Reason.CreateImageView);
			return imageView;
		}

		[MustUseReturnValue]
		public static VkImageView[] CreateImageViews(VkDevice logicalDevice, VkImage[] images, VkFormat imageFormat) {
			VkImageView[] imageViews = new VkImageView[images.Length];

			fixed (VkImageView* imageViewsPtr = imageViews) {
				for (int i = 0; i < images.Length; i++) {
					VkImageViewCreateInfo createInfo = new() {
							image = images[i],
							viewType = VkImageViewType.ImageViewType2d,
							format = imageFormat,
							components = new() {
									r = VkComponentSwizzle.ComponentSwizzleIdentity,
									g = VkComponentSwizzle.ComponentSwizzleIdentity,
									b = VkComponentSwizzle.ComponentSwizzleIdentity,
									a = VkComponentSwizzle.ComponentSwizzleIdentity,
							},
							subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
					};

					CheckIfSuccess(Vk.CreateImageView(logicalDevice, &createInfo, null, &imageViewsPtr[i]), VulkanException.Reason.CreateImageViews, i);
				}
			}

			return imageViews;
		}
	}
}