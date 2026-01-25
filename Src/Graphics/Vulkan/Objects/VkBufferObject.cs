using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	[PublicAPI]
	public unsafe class VkBufferObject : IVkBufferObject {
		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }
		public ulong BufferSize { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkPhysicalDevice physicalDevice;
		private readonly VkDevice logicalDevice;

		public VkBufferObject(string debugName, ulong bufferSize, VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			DebugName = debugName;
			this.physicalDevice = physicalDevice;
			this.logicalDevice = logicalDevice;
			BufferSize = bufferSize;

			Buffer = VkH.CreateBuffer(logicalDevice, bufferUsageFlags, bufferSize);
			BufferMemory = VkH.CreateDeviceMemory(physicalDevice, logicalDevice, Buffer, memoryPropertyFlags);
			VkH.BindBufferMemory(logicalDevice, Buffer, BufferMemory);
		}

		[MustUseReturnValue] public void* MapMemory(ulong bufferSize, ulong offset = 0) => VkH.MapMemory(logicalDevice, BufferMemory, bufferSize, offset);

		public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged => Copy(data, 0);
		public void Copy<T>(ReadOnlySpan<T> data, ulong offset) where T : unmanaged => VkH.MapAndCopyMemory(logicalDevice, BufferMemory, data, offset);

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged =>
				CopyUsingStaging(physicalDevice, logicalDevice, transferPool, transferQueue, Buffer, data, offset);

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			Vk.DestroyBuffer(logicalDevice, Buffer, null);
			Vk.FreeMemory(logicalDevice, BufferMemory, null);

			WasDestroyed = true;
		}

		public static void CopyUsingStaging<T>(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferPool, VkQueue transferQueue, VkBuffer dstBuffer, ReadOnlySpan<T> data, ulong offset = 0)
				where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * data.Length);

			VkBufferObject stagingBuffer = new("Temporary Staging Buffer", bufferSize, physicalDevice, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferSrcBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit); // TODO should i make a persistent staging buffer?

			VkH.MapAndCopyMemory(logicalDevice, stagingBuffer.BufferMemory, data, offset);

			TransferCommandBufferObject transferCommandBuffer = new(logicalDevice, transferPool, transferQueue);

			transferCommandBuffer.BeginCommandBuffer(VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit);
			transferCommandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, dstBuffer, bufferSize);
			transferCommandBuffer.EndCommandBuffer();
			transferCommandBuffer.SubmitQueue();

			transferCommandBuffer.Destroy();

			stagingBuffer.Destroy();
		}
	}
}