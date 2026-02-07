using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	[PublicAPI]
	public sealed unsafe class DescriptorBuffers : NamedGraphicsResource<DescriptorBuffers, ulong> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkDescriptorType DescriptorType { get; }

		public ulong BufferSize { get; }

		protected override ulong Handle => buffers[0].Buffer.Handle;

		private readonly VulkanBuffer[] buffers;
		private readonly void*[] buffersMapped;
		private readonly LogicalGpu logicalGpu;

		internal DescriptorBuffers(string debugName, LogicalGpu logicalGpu, ulong bufferSize, byte maxFramesInFlight, VkBufferUsageFlagBits bufferUsageFlags, VkDescriptorType descriptorType) : base(debugName) {
			BufferSize = bufferSize;
			DescriptorType = descriptorType;
			this.logicalGpu = logicalGpu;

			buffers = new VulkanBuffer[maxFramesInFlight];
			buffersMapped = new void*[maxFramesInFlight];

			for (int i = 0; i < maxFramesInFlight; i++) {
				VulkanBuffer buffer = logicalGpu.CreateBuffer($"{debugName}[{i}]", bufferUsageFlags, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, bufferSize);
				buffers[i] = buffer;
				buffersMapped[i] = buffer.MapMemory(bufferSize);
			}

			PrintCreate();
		}

		public void Copy<T>(T data, byte frameIndex) where T : unmanaged => Buffer.MemoryCopy(&data, buffersMapped[frameIndex], sizeof(T), sizeof(T));

		public void Copy<T>(ReadOnlySpan<T> data, byte frameIndex) where T : unmanaged {
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, buffersMapped[frameIndex], (ulong)data.Length, (ulong)data.Length); }
		}

		public void Copy<T>(ReadOnlySpan<T> data, byte frameIndex, ulong offset) where T : unmanaged {
#if DEBUG
			checked { // is this safe? untested
				fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, buffersMapped[frameIndex], (ulong)data.Length, (ulong)data.Length); }
			}
#else
			fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, buffersMapped[frameIndex], (ulong)data.Length, (ulong)data.Length); }
#endif
		}

		public VkBuffer GetBuffer(byte index) => buffers[index].Buffer;

		protected override void Cleanup() {
			foreach (VulkanBuffer buffer in buffers) { logicalGpu.EnqueueDestroy(buffer); }
		}
	}
}