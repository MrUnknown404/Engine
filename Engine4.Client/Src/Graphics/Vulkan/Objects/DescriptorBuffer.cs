using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

[Obsolete]
public unsafe class DescriptorBuffer : NamedVulkanResource {
	public VkDescriptorType DescriptorType { get; }
	public VulkanBuffer Buffer { get; }

	public override ulong Handle => Buffer.VkBuffer.Handle;

	private readonly void* bufferMapped;
	private readonly VulkanResourceManager resourceManager;

	public DescriptorBuffer(string debugName, VulkanResourceManager resourceManager, ulong bufferSize, VkBufferUsageFlagBits2 bufferUsageFlags, VkDescriptorType descriptorType) : base(debugName) {
		DescriptorType = descriptorType;
		Buffer = resourceManager.CreateBuffer($"{debugName}", bufferSize, bufferUsageFlags, 0, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);
		bufferMapped = Buffer.MapMemory(bufferSize);
		this.resourceManager = resourceManager;
	}

	public void Copy<T>(T data) where T : unmanaged => System.Buffer.MemoryCopy(&data, bufferMapped, Buffer.Size, (ulong)sizeof(T));

	public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged {
		fixed (void* dataPtr = data) { System.Buffer.MemoryCopy(dataPtr, bufferMapped, Buffer.Size, (ulong)(data.Length * sizeof(T))); }
	}

	public void Copy<T>(ReadOnlySpan<T> data, ulong offset) where T : unmanaged {
#if DEBUG
		checked { // is this safe? untested
			fixed (void* dataPtr = data[(int)offset..]) { System.Buffer.MemoryCopy(dataPtr, bufferMapped, Buffer.Size, (ulong)(data.Length * sizeof(T))); }
		}
#else
			fixed (void* dataPtr = data[(int)offset..]) { System.Buffer.MemoryCopy(dataPtr, bufferMapped, Buffer.Size, (ulong)(data.Length * sizeof(T))); }
#endif
	}

	protected internal override void Cleanup() => resourceManager.EnqueueDestroy(Buffer);
}