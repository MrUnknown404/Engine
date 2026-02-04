using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	[PublicAPI]
	public unsafe class DescriptorBuffers : INamedGraphicsResource, IEquatable<DescriptorBuffers> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkDescriptorType DescriptorType { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public ulong BufferSize { get; }

		private readonly VulkanRenderer renderer;
		private readonly VulkanBuffer[] buffers;
		private readonly void*[] buffersMapped;

		internal DescriptorBuffers(string debugName, ulong bufferSize, VulkanRenderer renderer, VulkanBuffer[] buffers, void*[] buffersMapped, VkDescriptorType descriptorType) {
			DebugName = debugName;
			BufferSize = bufferSize;
			this.renderer = renderer;
			this.buffers = buffers;
			this.buffersMapped = buffersMapped;
			DescriptorType = descriptorType;
		}

		public void Copy<T>(T data) where T : unmanaged => Buffer.MemoryCopy(&data, buffersMapped[renderer.FrameIndex], sizeof(T), sizeof(T));

		public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged {
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, buffersMapped[renderer.FrameIndex], (ulong)data.Length, (ulong)data.Length); }
		}

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset) where T : unmanaged {
#if DEBUG
			checked { // is this safe? untested
				fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, buffersMapped[renderer.FrameIndex], (ulong)data.Length, (ulong)data.Length); }
			}
#else
			fixed (void* dataPtr = data[(int)offset..]) { Buffer.MemoryCopy(dataPtr, buffersMapped[renderer.FrameIndex], (ulong)data.Length, (ulong)data.Length); }
#endif
		}

		public VkBuffer GetBuffer(byte index) => buffers[index].Buffer;

		public void Destroy() {
			if (INamedGraphicsResource.WarnIfDestroyed(this)) { return; }

			foreach (VulkanBuffer uniformBuffer in buffers) { uniformBuffer.Destroy(); }

			WasDestroyed = true;
		}

		public bool Equals(DescriptorBuffers? other) => other != null && buffers[0] == other.buffers[0];
		public override bool Equals(object? obj) => obj is DescriptorBuffers buffer && Equals(buffer);

		public override int GetHashCode() => buffers[0].GetHashCode();

		public static bool operator ==(DescriptorBuffers? left, DescriptorBuffers? right) => Equals(left, right);
		public static bool operator !=(DescriptorBuffers? left, DescriptorBuffers? right) => !Equals(left, right);
	}
}