using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class VkBufferObject : IBufferObject<ulong> {
		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }
		public ulong BufferSize { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkPhysicalDevice physicalDevice;
		private readonly VkDevice logicalDevice;

		public VkBufferObject(string debugName, VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) {
			DebugName = debugName;
			this.physicalDevice = physicalDevice;
			this.logicalDevice = logicalDevice;
			BufferSize = bufferSize;
			Buffer = CreateBuffer(logicalDevice, bufferUsageFlags, bufferSize);
			BufferMemory = CreateDeviceMemory(physicalDevice, logicalDevice, Buffer, memoryPropertyFlags);
			BindBufferMemory(logicalDevice, Buffer, BufferMemory);
		}

		[MustUseReturnValue] public void* MapMemory(ulong bufferSize, ulong offset = 0) => MapMemory(logicalDevice, BufferMemory, bufferSize, offset);

		public void Copy<T>(T[] data, ulong offset = 0) where T : unmanaged => MapAndCopyMemory(logicalDevice, BufferMemory, data, offset);

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, T[] data, ulong offset = 0) where T : unmanaged {
			VkBufferObject stagingBuffer = new("StagingBuffer", physicalDevice, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, BufferSize); // TODO should i make a persistent staging buffer?

			MapAndCopyMemory(logicalDevice, stagingBuffer.BufferMemory, data, offset);
			CopyBuffer(logicalDevice, transferQueue, transferPool, stagingBuffer.Buffer, Buffer, BufferSize);

			stagingBuffer.Destroy();
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroyBuffer(logicalDevice, Buffer, null);
			Vk.FreeMemory(logicalDevice, BufferMemory, null);

			WasDestroyed = true;
		}

		[MustUseReturnValue]
		private static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsage, ulong size, uint[]? queueFamilyIndices = null) {
			if (queueFamilyIndices == null) { return CreateBuffer(logicalDevice, new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeExclusive, }); }

			fixed (uint* queueFamilyIndicesPtr = queueFamilyIndices) {
				return CreateBuffer(logicalDevice,
					new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeConcurrent, queueFamilyIndexCount = (uint)queueFamilyIndices.Length, pQueueFamilyIndices = queueFamilyIndicesPtr, });
			}

			[MustUseReturnValue]
			static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferCreateInfo bufferCreateInfo) {
				VkBuffer buffer;
				VkResult result = Vk.CreateBuffer(logicalDevice, &bufferCreateInfo, null, &buffer);
				return result != VkResult.Success ? throw new VulkanException($"Failed to create buffer. {result}") : buffer;
			}
		}

		[MustUseReturnValue]
		private static VkDeviceMemory CreateDeviceMemory(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBuffer buffer, VkMemoryPropertyFlagBits memoryPropertyFlags) {
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

		[MustUseReturnValue]
		private static void* MapMemory(VkDevice logicalDevice, VkDeviceMemory deviceMemory, ulong bufferSize, ulong offset) {
			VkMemoryMapInfo memoryMapInfo = new() { memory = deviceMemory, size = bufferSize, offset = offset, };
			void* dataPtr;
			Vk.MapMemory2(logicalDevice, &memoryMapInfo, &dataPtr);
			return dataPtr;
		}

		private static void CopyMemory(void* srcDataPtr, void* dstDataPtr, ulong dstSize, ulong sourceBytesToCopy) => System.Buffer.MemoryCopy(srcDataPtr, dstDataPtr, dstSize, sourceBytesToCopy);

		private static void UnmapMemory(VkDevice logicalDevice, VkDeviceMemory deviceMemory) {
			VkMemoryUnmapInfo memoryUnmapInfo = new() { memory = deviceMemory, };
			Vk.UnmapMemory2(logicalDevice, &memoryUnmapInfo);
		}

		private static void BindBufferMemory(VkDevice logicalDevice, VkBuffer buffer, VkDeviceMemory deviceMemory) {
			VkBindBufferMemoryInfo bindBufferMemoryInfo = new() { buffer = buffer, memory = deviceMemory, };
			VkResult result = Vk.BindBufferMemory2(logicalDevice, 1, &bindBufferMemoryInfo);
			if (result != VkResult.Success) { throw new VulkanException($"Failed to bind buffer memory. {result}"); }
		}

		private static void MapAndCopyMemory<T>(VkDevice logicalDevice, VkDeviceMemory deviceMemory, T[] inData, ulong offset) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * inData.Length);
			fixed (T* inDataPtr = inData) {
				void* dataPtr = MapMemory(logicalDevice, deviceMemory, bufferSize, offset);
				CopyMemory(inDataPtr, dataPtr, bufferSize, bufferSize);
				UnmapMemory(logicalDevice, deviceMemory);
			}
		}

		private static void CopyBuffer(VkDevice logicalDevice, VkQueue transferQueue, VkCommandPool transferCommandPool, VkBuffer srcBuffer, VkBuffer dstBuffer, ulong bufferSize) {
			TransferCommandBuffer transferCommandBuffer = new(logicalDevice, transferCommandPool);

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);
			transferCommandBuffer.CmdCopyBuffer(srcBuffer, dstBuffer, bufferSize);
			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			Vk.QueueWaitIdle(transferQueue);
			transferCommandBuffer.FreeCommandBuffers();
		}

		private static void CopyBuffers(VkDevice logicalDevice, VkQueue transferQueue, VkCommandPool transferCommandPool, VkBuffer[] srcBuffers, VkBuffer[] dstBuffers, ulong[] bufferSizes) {
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
	}
}