using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Client.Graphics.Vulkan.Objects;

public sealed unsafe class VulkanBuffer : NamedGraphicsResource<VulkanBuffer, ulong> {
	public VkBuffer Buffer { get; }
	public VkDeviceMemory Memory { get; }

	public VkBufferUsageFlagBits UsageFlags { get; }
	public VkMemoryPropertyFlagBits MemoryPropertyFlags { get; }

	public ulong BufferSize { get; }

	protected override ulong Handle => Buffer.Handle;

	private readonly LogicalGpu logicalGpu;

	internal VulkanBuffer(string debugName, LogicalGpu logicalGpu, VkBuffer buffer, VkDeviceMemory memory, VkBufferUsageFlagBits usageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong bufferSize) : base(debugName) {
		Buffer = buffer;
		Memory = memory;
		BufferSize = bufferSize;
		UsageFlags = usageFlags;
		MemoryPropertyFlags = memoryPropertyFlags;
		this.logicalGpu = logicalGpu;

		PrintCreate();
	}

	public void Copy<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged {
		fixed (T* dataPtr = data) { Copy(dataPtr, (ulong)(data.Length * sizeof(T)), offset); }
	}

	public void Copy(ReadOnlySpan<byte> data, ulong offset = 0) {
		fixed (byte* dataPtr = data) { Copy(dataPtr, (ulong)data.Length, offset); }
	}

	public void Copy(void* data, ulong bufferSize, ulong offset = 0) {
		void* dstPtr = MapMemory(bufferSize, offset);
		System.Buffer.MemoryCopy(data, dstPtr, bufferSize, bufferSize);
		UnmapMemory();
	}

	[MustUseReturnValue]
	public void* MapMemory(ulong bufferSize, ulong offset = 0) {
		VkMemoryMapInfo memoryMapInfo = new() { memory = Memory, size = bufferSize, offset = offset, };
		void* dstPtr;
		Vk.MapMemory2(logicalGpu.LogicalDevice, &memoryMapInfo, &dstPtr);
		return dstPtr;
	}

	public void UnmapMemory() {
		VkMemoryUnmapInfo memoryUnmapInfo = new() { memory = Memory, };
		Vk.UnmapMemory2(logicalGpu.LogicalDevice, &memoryUnmapInfo);
	}

	protected override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.LogicalDevice;

		Vk.DestroyBuffer(logicalDevice, Buffer, null);
		Vk.FreeMemory(logicalDevice, Memory, null);
	}
}