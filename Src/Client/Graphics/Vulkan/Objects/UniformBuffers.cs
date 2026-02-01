using JetBrains.Annotations;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	[PublicAPI]
	public unsafe class UniformBuffers : INamedGraphicsResource, IEquatable<UniformBuffers> {
		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public ulong BufferSize { get; }

		private readonly VulkanRenderer renderer;
		private readonly VulkanBuffer[] buffers;
		private readonly void*[] buffersMapped;

		internal UniformBuffers(string debugName, ulong bufferSize, VulkanRenderer renderer, VulkanBuffer[] buffers, void*[] buffersMapped) {
			DebugName = debugName;
			BufferSize = bufferSize;
			this.renderer = renderer;
			this.buffers = buffers;
			this.buffersMapped = buffersMapped;

			foreach (VulkanBuffer buffer in buffers) { INamedGraphicsResource.PrintNameWithHandle(this, buffer.Buffer.Handle); }
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
			if (INamedGraphicsResource.WarnIfDestroyed(this)) { return; }

			foreach (VulkanBuffer uniformBuffer in buffers) { uniformBuffer.Destroy(); }

			WasDestroyed = true;
		}

		public bool Equals(UniformBuffers? other) => other != null && buffers[0] == other.buffers[0];
		public override bool Equals(object? obj) => obj is UniformBuffers buffer && Equals(buffer);

		public override int GetHashCode() => buffers[0].GetHashCode();

		public static bool operator ==(UniformBuffers? left, UniformBuffers? right) => Equals(left, right);
		public static bool operator !=(UniformBuffers? left, UniformBuffers? right) => !Equals(left, right);
	}
}