using Engine3.Exceptions;
using Engine3.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Api.Graphics {
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
		public static VkSemaphore[] CreateSemaphores(VkDevice logicalDevice, uint count) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore[] semaphores = new VkSemaphore[count];

			fixed (VkSemaphore* semaphoresPtr = semaphores) {
				for (int i = 0; i < count; i++) {
					VkResult result = Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]);
					if (result != VkResult.Success) { throw new VulkanException($"Failed to create semaphore. {result}"); }
				}
			}

			return semaphores;
		}

		[MustUseReturnValue]
		public static VkSemaphore CreateSemaphore(VkDevice logicalDevice) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore semaphore;
			VkResult result = Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphore);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create semaphore. {result}") : semaphore;
		}

		[MustUseReturnValue]
		public static VkFence[] CreateFences(VkDevice logicalDevice, uint count) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence[] fences = new VkFence[count];

			fixed (VkFence* fencesPtr = fences) {
				for (int i = 0; i < count; i++) {
					VkResult result = Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fencesPtr[i]);
					if (result != VkResult.Success) { throw new VulkanException($"Failed to create fence. {result}"); }
				}
			}

			return fences;
		}

		[MustUseReturnValue]
		public static VkFence CreateFence(VkDevice logicalDevice) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence fence;
			VkResult result = Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fence);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create fence. {result}") : fence;
		}

		[MustUseReturnValue]
		public static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferCreateInfo bufferCreateInfo) {
			VkBuffer buffer;
			VkResult result = Vk.CreateBuffer(logicalDevice, &bufferCreateInfo, null, &buffer);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create buffer. {result}") : buffer;
		}

		[MustUseReturnValue]
		public static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsage, ulong size, uint[]? queueFamilyIndices = null) {
			if (queueFamilyIndices == null) { return VkH.CreateBuffer(logicalDevice, new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeExclusive, }); }

			fixed (uint* queueFamilyIndicesPtr = queueFamilyIndices) {
				return VkH.CreateBuffer(logicalDevice,
					new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeConcurrent, queueFamilyIndexCount = (uint)queueFamilyIndices.Length, pQueueFamilyIndices = queueFamilyIndicesPtr, });
			}
		}

		[MustUseReturnValue]
		public static VkDeviceMemory CreateDeviceMemory(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBuffer buffer, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkBufferMemoryRequirementsInfo2 bufferMemoryRequirementsInfo2 = new() { buffer = buffer, };
			VkMemoryRequirements2 memoryRequirements2 = new();
			Vk.GetBufferMemoryRequirements2(logicalDevice, &bufferMemoryRequirementsInfo2, &memoryRequirements2);
			VkMemoryRequirements memoryRequirements = memoryRequirements2.memoryRequirements;

			VkMemoryAllocateInfo memoryAllocateInfo = new() { allocationSize = memoryRequirements.size, memoryTypeIndex = FindMemoryType(physicalDevice, memoryRequirements.memoryTypeBits, memoryPropertyFlags), };
			VkDeviceMemory deviceMemory;

			// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer.
			// The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation
			// among many different objects by using the offset parameters that we've seen in many functions."
			VkResult result = Vk.AllocateMemory(logicalDevice, &memoryAllocateInfo, null, &deviceMemory);
			return result != VkResult.Success ? throw new VulkanException($"Failed to allocate memory. {result}") : deviceMemory;

			[MustUseReturnValue]
			static uint FindMemoryType(VkPhysicalDevice physicalDevice, uint typeFilter, VkMemoryPropertyFlagBits memoryPropertyFlag) {
				VkPhysicalDeviceMemoryProperties2 memoryProperties2 = new();
				Vk.GetPhysicalDeviceMemoryProperties2(physicalDevice, &memoryProperties2);
				VkPhysicalDeviceMemoryProperties memoryProperties = memoryProperties2.memoryProperties;

				for (uint i = 0; i < memoryProperties.memoryTypeCount; i++) {
					if ((uint)(typeFilter & (1 << (int)i)) != 0 && (memoryProperties.memoryTypes[(int)i].propertyFlags & memoryPropertyFlag) == memoryPropertyFlag) { return i; }
				}

				throw new VulkanException("Failed to find suitable memory type");
			}
		}

		public static void CopyBuffer(VkDevice logicalDevice, VkQueue transferQueue, VkCommandPool transferCommandPool, VkBuffer srcBuffer, VkBuffer dstBuffer, ulong bufferSize) {
			TransferCommandBuffer transferCommandBuffer = new(logicalDevice, transferCommandPool);

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);
			transferCommandBuffer.CmdCopyBuffer(srcBuffer, dstBuffer, bufferSize);
			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			Vk.QueueWaitIdle(transferQueue);
			transferCommandBuffer.FreeCommandBuffers();
		}

		public static void CopyBuffers(VkDevice logicalDevice, VkQueue transferQueue, VkCommandPool transferCommandPool, VkBuffer[] srcBuffers, VkBuffer[] dstBuffers, ulong[] bufferSizes) {
			int size = srcBuffers.Length;
			if (dstBuffers.Length != size || bufferSizes.Length != size) { throw new VulkanException("All srcBuffers/dstBuffers/bufferSizes must be the same length"); }

			TransferCommandBuffer transferCommandBuffer = new(logicalDevice, transferCommandPool);

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);
			for (int i = 0; i < srcBuffers.Length; i++) { transferCommandBuffer.CmdCopyBuffer(srcBuffers[i], dstBuffers[i], bufferSizes[i]); }
			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			Vk.QueueWaitIdle(transferQueue);
			transferCommandBuffer.FreeCommandBuffers();
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

		public static void MapAndCopyMemory<T>(VkDevice logicalDevice, VkDeviceMemory deviceMemory, T[] inData, ulong offset) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * inData.Length);
			fixed (T* inDataPtr = inData) {
				void* dataPtr = VkH.MapMemory(logicalDevice, deviceMemory, bufferSize, offset);
				VkH.CopyMemory(inDataPtr, dataPtr, bufferSize, bufferSize);
				VkH.UnmapMemory(logicalDevice, deviceMemory);
			}
		}

		public static void BindBufferMemory(VkDevice logicalDevice, VkBuffer buffer, VkDeviceMemory deviceMemory) {
			VkBindBufferMemoryInfo bindBufferMemoryInfo = new() { buffer = buffer, memory = deviceMemory, };
			VkResult result = Vk.BindBufferMemory2(logicalDevice, 1, &bindBufferMemoryInfo);
			if (result != VkResult.Success) { throw new VulkanException($"Failed to bind buffer memory. {result}"); }
		}
	}
}