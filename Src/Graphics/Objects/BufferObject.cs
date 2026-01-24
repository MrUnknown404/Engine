using Engine3.Exceptions;
using Engine3.Graphics.OpenGL.Objects;
using Engine3.Graphics.Vulkan;
using Engine3.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Objects {
	[PublicAPI]
	public unsafe class BufferObject : IGlBufferObject, IVkBufferObject {
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
				case GraphicsBackend.OpenGL: GL.NamedBufferSubData((int)Handle, 0, sizeof(T) * data.Length, data); break;
				case GraphicsBackend.Vulkan: VkH.MapAndCopyMemory(logicalDevice, BufferMemory, data, 0); break;
				case GraphicsBackend.Console: throw new IllegalStateException();
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public void Copy<T>(T[] data, nint offset) where T : unmanaged {
			switch (graphicsBackend) {
				case GraphicsBackend.OpenGL: GL.NamedBufferSubData((int)Handle, offset, sizeof(T) * data.Length, data); break;
				case GraphicsBackend.Vulkan:
#if DEBUG
					checked { VkH.MapAndCopyMemory(logicalDevice, BufferMemory, data, (ulong)offset); }
#else
					VkH.MapAndCopyMemory(logicalDevice, BufferMemory, data, (ulong)offset);
#endif
					break;
				case GraphicsBackend.Console: throw new IllegalStateException();
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public void Copy<T>(T[] data, ulong offset) where T : unmanaged {
			switch (graphicsBackend) {
				case GraphicsBackend.OpenGL:
#if DEBUG
					checked { GL.NamedBufferSubData((int)Handle, (nint)offset, sizeof(T) * data.Length, data); }
#else
					GL.NamedBufferSubData((int)Handle, (nint)offset, sizeof(T) * data.Length, data);
#endif
					break;
				case GraphicsBackend.Vulkan: VkH.MapAndCopyMemory(logicalDevice, BufferMemory, data, offset); break;
				case GraphicsBackend.Console: throw new IllegalStateException();
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, T[] data, nint offset = 0) where T : unmanaged {
			switch (graphicsBackend) {
				case GraphicsBackend.OpenGL: throw new NotImplementedException();
				case GraphicsBackend.Vulkan:
#if DEBUG
					checked { VkBufferObject.CopyUsingStaging(physicalDevice, logicalDevice, transferPool, transferQueue, Buffer, data, (ulong)offset); }
#else
					VkBufferObject.CopyUsingStaging(physicalDevice, logicalDevice, transferPool, transferQueue, Buffer, data, (ulong)offset);
#endif
					break;
				case GraphicsBackend.Console: throw new IllegalStateException();
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public void CopyUsingStaging<T>(VkCommandPool transferPool, VkQueue transferQueue, T[] data, ulong offset = 0) where T : unmanaged {
			switch (graphicsBackend) {
				case GraphicsBackend.OpenGL: throw new NotImplementedException();
				case GraphicsBackend.Vulkan: VkBufferObject.CopyUsingStaging(physicalDevice, logicalDevice, transferPool, transferQueue, Buffer, data, offset); break;
				case GraphicsBackend.Console: throw new IllegalStateException();
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			switch (graphicsBackend) {
				case GraphicsBackend.OpenGL: GL.DeleteBuffer((int)Handle); break;
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