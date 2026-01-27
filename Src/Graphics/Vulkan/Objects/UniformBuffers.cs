using Engine3.Graphics.Objects;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class UniformBuffers : IBufferObject {
		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkRenderer renderer;
		private readonly VkBufferObject[] uniformBuffers;
		private readonly void*[] uniformBuffersMapped;

		public UniformBuffers(string debugName, VkRenderer renderer, VkPhysicalDeviceMemoryProperties2 memoryProperties, VkDevice logicalDevice, ulong bufferSize) {
			DebugName = debugName;
			this.renderer = renderer;
			uniformBuffers = new VkBufferObject[renderer.MaxFramesInFlight];
			uniformBuffersMapped = new void*[renderer.MaxFramesInFlight];

			for (int i = 0; i < renderer.MaxFramesInFlight; i++) {
				VkBufferObject uniformBuffer = new($"Test Uniform Buffer[{i}]", bufferSize, memoryProperties, logicalDevice, VkBufferUsageFlagBits.BufferUsageUniformBufferBit,
					VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

				uniformBuffers[i] = uniformBuffer;
				uniformBuffersMapped[i] = uniformBuffer.MapMemory(bufferSize);
			}
		}

		public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged {
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[renderer.CurrentFrame], (ulong)data.Length, (ulong)data.Length); }
		}

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset) where T : unmanaged {
#if DEBUG
			checked {
				fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[renderer.CurrentFrame], (ulong)data.Length, (ulong)data.Length); }
			}
#else
			fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[renderer.CurrentFrame], (ulong)data.Length, (ulong)data.Length); }
#endif
		}

		public VkBuffer GetBuffer(byte index) => uniformBuffers[index].Buffer;

		public void Destroy() {
			IGraphicsResource.WarnIfDestroyed(this);

			foreach (VkBufferObject uniformBuffer in uniformBuffers) { uniformBuffer.Destroy(); }

			WasDestroyed = true;
		}
	}
}