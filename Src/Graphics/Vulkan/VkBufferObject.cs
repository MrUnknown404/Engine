using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public unsafe class VkBufferObject {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }
		public ulong BufferSize { get; }

		private readonly VkPhysicalDevice physicalDevice;
		private readonly VkDevice logicalDevice;
		private bool wasDestroyed;

		public VkBufferObject(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) {
			this.physicalDevice = physicalDevice;
			this.logicalDevice = logicalDevice;
			BufferSize = bufferSize;
			Buffer = VkH.CreateBuffer(logicalDevice, bufferUsageFlags, bufferSize);
			BufferMemory = VkH.CreateDeviceMemory(physicalDevice, logicalDevice, Buffer, memoryPropertyFlags);
			BindBufferMemory(logicalDevice, Buffer, BufferMemory);
		}

		[MustUseReturnValue] public void* MapMemory(ulong bufferSize) => MapMemory(logicalDevice, BufferMemory, bufferSize);

		public void Copy<T>(T[] data) where T : unmanaged => MapAndCopyMemory(logicalDevice, BufferMemory, data);

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, T[] data) where T : unmanaged {
			VkBuffer stagingBuffer = VkH.CreateBuffer(logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferSrcBit, BufferSize); // TODO should i make a persistent staging buffer?
			VkDeviceMemory stagingBufferMemory = VkH.CreateDeviceMemory(physicalDevice, logicalDevice, stagingBuffer, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

			BindBufferMemory(logicalDevice, stagingBuffer, stagingBufferMemory);
			MapAndCopyMemory(logicalDevice, stagingBufferMemory, data);
			CopyBuffer(logicalDevice, transferQueue, transferPool, stagingBuffer, Buffer, BufferSize);

			Vk.DestroyBuffer(logicalDevice, stagingBuffer, null);
			Vk.FreeMemory(logicalDevice, stagingBufferMemory, null);
		}

		public void Destroy() {
			if (wasDestroyed) {
				Logger.Warn($"{nameof(SwapChain)} was already destroyed");
				return;
			}

			Vk.DestroyBuffer(logicalDevice, Buffer, null);
			Vk.FreeMemory(logicalDevice, BufferMemory, null);

			wasDestroyed = true;
		}

		[MustUseReturnValue]
		private static void* MapMemory(VkDevice logicalDevice, VkDeviceMemory deviceMemory, ulong bufferSize) {
			VkMemoryMapInfo memoryMapInfo = new() { memory = deviceMemory, size = bufferSize, };
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

		private static void MapAndCopyMemory<T>(VkDevice logicalDevice, VkDeviceMemory deviceMemory, T[] inData) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * inData.Length);
			fixed (T* inDataPtr = inData) {
				void* dataPtr = MapMemory(logicalDevice, deviceMemory, bufferSize);
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