using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	[PublicAPI]
	public unsafe class VulkanBuffer : IBufferObject, IEquatable<VulkanBuffer> {
		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }
		public ulong BufferSize { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly LogicalGpu logicalGpu;

		internal VulkanBuffer(string debugName, LogicalGpu logicalGpu, VkBuffer buffer, VkDeviceMemory bufferMemory, ulong bufferSize) {
			DebugName = debugName;
			Buffer = buffer;
			BufferMemory = bufferMemory;
			BufferSize = bufferSize;
			this.logicalGpu = logicalGpu;

			INamedGraphicsResource.PrintNameWithHandle(this, Buffer.Handle);
		}

		public static void CopyMemory(void* srcDataPtr, void* dstDataPtr, ulong dstSize, ulong sourceBytesToCopy) => System.Buffer.MemoryCopy(srcDataPtr, dstDataPtr, dstSize, sourceBytesToCopy);

		public void UnmapMemory() {
			VkMemoryUnmapInfo memoryUnmapInfo = new() { memory = BufferMemory, };
			Vk.UnmapMemory2(logicalGpu.LogicalDevice, &memoryUnmapInfo);
		}

		[MustUseReturnValue]
		public void* MapMemory(ulong bufferSize, ulong offset = 0) {
			VkMemoryMapInfo memoryMapInfo = new() { memory = BufferMemory, size = bufferSize, offset = offset, };
			void* dataPtr;
			Vk.MapMemory2(logicalGpu.LogicalDevice, &memoryMapInfo, &dataPtr);
			return dataPtr;
		}

		public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged => Copy(data, 0);

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * data.Length);
			fixed (T* inDataPtr = data) {
				void* dataPtr = MapMemory(bufferSize, offset);
				CopyMemory(inDataPtr, dataPtr, bufferSize, bufferSize);
				UnmapMemory();
			}
		}

		public void CopyUsingStaging<T>(VkCommandPool transferCommandPool, VkQueue transferQueue, ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * data.Length);

			VulkanBuffer stagingBuffer = logicalGpu.CreateBuffer("Temporary Staging Buffer", VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize); // TODO should i make a persistent staging buffer?

			stagingBuffer.Copy(data, offset);

			TransferCommandBuffer transferCommandBuffer = logicalGpu.CreateTransferCommandBuffer(transferCommandPool);

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);
			transferCommandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, Buffer, bufferSize);
			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue(transferQueue);

			Vk.QueueWaitIdle(transferQueue);
			transferCommandBuffer.Destroy();

			stagingBuffer.Destroy();
		}

		public void Destroy() {
			if (INamedGraphicsResource.WarnIfDestroyed(this)) { return; }

			VkDevice logicalDevice = logicalGpu.LogicalDevice;

			Vk.DestroyBuffer(logicalDevice, Buffer, null);
			Vk.FreeMemory(logicalDevice, BufferMemory, null);

			WasDestroyed = true;
		}

		public bool Equals(VulkanBuffer? other) => other != null && Buffer == other.Buffer;
		public override bool Equals(object? obj) => obj is VulkanBuffer buffer && Equals(buffer);

		public override int GetHashCode() => Buffer.GetHashCode();

		public static bool operator ==(VulkanBuffer? left, VulkanBuffer? right) => Equals(left, right);
		public static bool operator !=(VulkanBuffer? left, VulkanBuffer? right) => !Equals(left, right);
	}
}