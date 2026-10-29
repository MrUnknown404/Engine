using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

[Obsolete]
public unsafe class DescriptorBuffers : VulkanResource {
	public VkDescriptorType DescriptorType { get; }

	public ulong BufferSize { get; }

	public override ulong Handle => buffers[0].VkBuffer.Handle;

	private readonly VulkanBuffer[] buffers;
	private readonly void*[] buffersMapped;
	private readonly VulkanResourceManager resourceManager;

	internal DescriptorBuffers(string debugName, VulkanResourceManager resourceManager, ulong bufferSize, byte maxFramesInFlight, VkBufferUsageFlagBits2 bufferUsageFlags, VkDescriptorType descriptorType) :
			base(debugName) {
		BufferSize = bufferSize;
		DescriptorType = descriptorType;
		this.resourceManager = resourceManager;

		buffers = new VulkanBuffer[maxFramesInFlight];
		buffersMapped = new void*[maxFramesInFlight];

		for (int i = 0; i < maxFramesInFlight; i++) {
			VulkanBuffer buffer = resourceManager.CreateBuffer($"{debugName}[{i}]", bufferSize, bufferUsageFlags, 0,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

			buffers[i] = buffer;
			buffersMapped[i] = buffer.MapMemory(bufferSize);
		}
	}

	public void Copy<T>(T data, byte frameIndex) where T : unmanaged => Buffer.MemoryCopy(&data, buffersMapped[frameIndex], BufferSize, (ulong)sizeof(T));

	public void Copy<T>(ReadOnlySpan<T> data, byte frameIndex) where T : unmanaged {
		fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, buffersMapped[frameIndex], BufferSize, (ulong)(data.Length * sizeof(T))); }
	}

	public void Copy<T>(ReadOnlySpan<T> data, byte frameIndex, ulong offset) where T : unmanaged {
#if DEBUG
		checked { // is this safe? untested
			fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, buffersMapped[frameIndex], BufferSize, (ulong)(data.Length * sizeof(T))); }
		}
#else
			fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, buffersMapped[frameIndex], BufferSize,  (ulong)(data.Length * sizeof(T))); }
#endif
	}

	public VulkanBuffer GetBuffer(byte index) => buffers[index];

	protected internal override void Cleanup() {
		foreach (VulkanBuffer buffer in buffers) { resourceManager.EnqueueDestroy(buffer); }
	}
}