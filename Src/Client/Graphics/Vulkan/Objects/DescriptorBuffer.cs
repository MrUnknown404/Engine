using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	[PublicAPI]
	public sealed unsafe class DescriptorBuffer : NamedGraphicsResource<DescriptorBuffer, ulong> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkDescriptorType DescriptorType { get; }
		public VulkanBuffer Buffer { get; }

		protected override ulong Handle => Buffer.Buffer.Handle;

		private readonly void* bufferMapped;
		private readonly VulkanResourceProvider graphicsResourceProvider;

		public DescriptorBuffer(string debugName, VulkanResourceProvider graphicsResourceProvider, ulong bufferSize, VkBufferUsageFlagBits bufferUsageFlags, VkDescriptorType descriptorType) : base(debugName) {
			DescriptorType = descriptorType;
			Buffer = graphicsResourceProvider.CreateBuffer($"{debugName}", bufferUsageFlags, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize);
			bufferMapped = Buffer.MapMemory(bufferSize);
			this.graphicsResourceProvider = graphicsResourceProvider;

			PrintCreate();
		}

		public void Copy<T>(T data) where T : unmanaged => System.Buffer.MemoryCopy(&data, bufferMapped, Buffer.BufferSize, (ulong)sizeof(T));

		public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged {
			fixed (void* dataPtr = data) { System.Buffer.MemoryCopy(dataPtr, bufferMapped, Buffer.BufferSize, (ulong)(data.Length * sizeof(T))); }
		}

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset) where T : unmanaged {
#if DEBUG
			checked { // is this safe? untested
				fixed (void* dataPtr = data[(int)offset..]) { System.Buffer.MemoryCopy(dataPtr, bufferMapped, Buffer.BufferSize, (ulong)(data.Length * sizeof(T))); }
			}
#else
			fixed (void* dataPtr = data[(int)offset..]) { System.Buffer.MemoryCopy(dataPtr, bufferMapped, Buffer.BufferSize, (ulong)(data.Length * sizeof(T))); }
#endif
		}

		protected override void Cleanup() => graphicsResourceProvider.EnqueueDestroy(Buffer);
	}
}