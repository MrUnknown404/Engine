using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	[PublicAPI]
	public sealed unsafe class VulkanBuffer : NamedGraphicsResource<VulkanBuffer, ulong> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }
		public ulong BufferSize { get; }

		protected override ulong Handle => Buffer.Handle;

		private readonly LogicalGpu logicalGpu;

		internal VulkanBuffer(string debugName, LogicalGpu logicalGpu, VkBuffer buffer, VkDeviceMemory bufferMemory, ulong bufferSize) : base(debugName) {
			Buffer = buffer;
			BufferMemory = bufferMemory;
			BufferSize = bufferSize;
			this.logicalGpu = logicalGpu;

			PrintCreate();
		}

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * data.Length);
			fixed (T* inDataPtr = data) {
				void* dataPtr = MapMemory(bufferSize, offset);
				System.Buffer.MemoryCopy(inDataPtr, dataPtr, bufferSize, bufferSize);
				UnmapMemory();
			}
		}

		public void Copy(void* data, ulong bufferSize, ulong offset = 0) {
			void* dataPtr = MapMemory(bufferSize, offset);
			System.Buffer.MemoryCopy(data, dataPtr, bufferSize, bufferSize);
			UnmapMemory();
		}

		[MustUseReturnValue]
		public void* MapMemory(ulong bufferSize, ulong offset = 0) {
			VkMemoryMapInfo memoryMapInfo = new() { memory = BufferMemory, size = bufferSize, offset = offset, };
			void* dataPtr;
			Vk.MapMemory2(logicalGpu.LogicalDevice, &memoryMapInfo, &dataPtr);
			return dataPtr;
		}

		public void UnmapMemory() {
			VkMemoryUnmapInfo memoryUnmapInfo = new() { memory = BufferMemory, };
			Vk.UnmapMemory2(logicalGpu.LogicalDevice, &memoryUnmapInfo);
		}

		protected override void Cleanup() {
			VkDevice logicalDevice = logicalGpu.LogicalDevice;

			Vk.DestroyBuffer(logicalDevice, Buffer, null);
			Vk.FreeMemory(logicalDevice, BufferMemory, null);
		}
	}
}