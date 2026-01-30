namespace Engine3.Client.Graphics.Vulkan {
	public unsafe class UniformBuffers : IGraphicsResource {
		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public ulong BufferSize { get; }

		private readonly VkRenderer renderer;
		private readonly VkBuffer[] buffers;
		private readonly void*[] buffersMapped;

		internal UniformBuffers(string debugName, ulong bufferSize, VkRenderer renderer, VkBuffer[] buffers, void*[] buffersMapped) {
			DebugName = debugName;
			BufferSize = bufferSize;
			this.renderer = renderer;
			this.buffers = buffers;
			this.buffersMapped = buffersMapped;
		}

		public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged {
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, buffersMapped[renderer.FrameIndex], (ulong)data.Length, (ulong)data.Length); }
		}

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset) where T : unmanaged {
#if DEBUG
			checked { // is this safe? untested
				fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, buffersMapped[renderer.FrameIndex], (ulong)data.Length, (ulong)data.Length); }
			}
#else
			fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[renderer.FrameIndex], (ulong)data.Length, (ulong)data.Length); }
#endif
		}

		public OpenTK.Graphics.Vulkan.VkBuffer GetBuffer(byte index) => buffers[index].Buffer;

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			foreach (VkBuffer uniformBuffer in buffers) { uniformBuffer.Destroy(); }

			WasDestroyed = true;
		}
	}
}