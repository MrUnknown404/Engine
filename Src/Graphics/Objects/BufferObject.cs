using Engine3.Api.Graphics;
using Engine3.Api.Graphics.Objects;
using Engine3.Exceptions;
using Engine3.Graphics.Vulkan.Objects;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Objects {
	public unsafe class BufferObject : IBufferObject<ulong>, IGlBufferObject, IVkBufferObject {
		public string DebugName { get; }
		public ulong BufferSize { get; }

		public BufferHandle Handle { get; }

		public VkBuffer Buffer { get; }
		public VkDeviceMemory BufferMemory { get; }

		private readonly VkPhysicalDevice physicalDevice;
		private readonly VkDevice logicalDevice;

		public bool WasDestroyed { get; private set; }

		private readonly GraphicsBackend graphicsBackend;

		public BufferObject(string debugName, nint bufferSize, BufferStorageMask bufferStorageMask) {
			graphicsBackend = GraphicsBackend.OpenGL;
			DebugName = debugName;

#if DEBUG
			checked { BufferSize = (ulong)bufferSize; }
#else
			BufferSize = (ulong)bufferSize;
#endif

			Handle = new(GL.CreateBuffer());
			GL.NamedBufferStorage((int)Handle, bufferSize, IntPtr.Zero, bufferStorageMask);
		}

		public BufferObject(string debugName, ulong bufferSize, VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsageFlags, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			graphicsBackend = GraphicsBackend.Vulkan;
			DebugName = debugName;
			this.physicalDevice = physicalDevice;
			this.logicalDevice = logicalDevice;
			BufferSize = bufferSize;

			Buffer = VkH.CreateBuffer(logicalDevice, bufferUsageFlags, bufferSize);
			BufferMemory = VkH.CreateDeviceMemory(physicalDevice, logicalDevice, Buffer, memoryPropertyFlags);
			VkH.BindBufferMemory(logicalDevice, Buffer, BufferMemory);
		}

		public void Copy<T>(T[] data) where T : unmanaged {
			switch (graphicsBackend) {
				case GraphicsBackend.OpenGL: Copy(data, (nint)0); break;
				case GraphicsBackend.Vulkan: Copy(data, (ulong)0); break;
				case GraphicsBackend.Console: throw new IllegalStateException();
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public void Copy<T>(T[] data, IntPtr offset) where T : unmanaged => GL.NamedBufferSubData((int)Handle, offset, sizeof(T) * data.Length, data);
		public void Copy<T>(T[] data, ulong offset) where T : unmanaged => VkH.MapAndCopyMemory(logicalDevice, BufferMemory, data, offset);

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, T[] data, ulong offset = 0) where T : unmanaged =>
				VkBufferObject.CopyUsingStaging(physicalDevice, logicalDevice, transferPool, transferQueue, Buffer, data, offset);

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			switch (graphicsBackend) {
				case GraphicsBackend.OpenGL: break;
				case GraphicsBackend.Vulkan:
					Vk.DestroyBuffer(logicalDevice, Buffer, null);
					Vk.FreeMemory(logicalDevice, BufferMemory, null);
					break;
				case GraphicsBackend.Console: throw new IllegalStateException();
				default: throw new ArgumentOutOfRangeException();
			}

			WasDestroyed = true;
		}
	}
}