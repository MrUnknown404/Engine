namespace Engine4.Client.Graphics.Vulkan.Objects;

public abstract class DescriptorHeap : VulkanResource {
	public abstract VulkanBuffer HeapBuffer { get; }
	public override ulong Handle => HeapBuffer.Handle;

	public abstract ulong DeviceAddress { get; }
	public abstract ulong MinHeapReservedRange { get; }

	private readonly VulkanResourceManager resourceManager;

	protected DescriptorHeap(VulkanResourceManager resourceManager) => this.resourceManager = resourceManager;

	protected internal override void Cleanup() => resourceManager.EnqueueDestroy(HeapBuffer);

	protected static ulong AlignSize(ulong value, ulong alignment) => (value + alignment - 1) & ~(alignment - 1);
}