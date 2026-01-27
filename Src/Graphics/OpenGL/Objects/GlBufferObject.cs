using JetBrains.Annotations;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.OpenGL.Objects {
	[PublicAPI]
	public unsafe class GlBufferObject : IGlBufferObject {
		public BufferHandle Handle { get; }
		public nint BufferSize { get; } // Closest to GLsizeiptr is nint? - https://wikis.khronos.org/opengl/OpenGL_Type

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public GlBufferObject(string debugName, nint bufferSize, BufferStorageMask bufferStorageMask) {
			DebugName = debugName;
			BufferSize = bufferSize;
			Handle = new(GL.CreateBuffer());

			GL.NamedBufferStorage((int)Handle, BufferSize, IntPtr.Zero, bufferStorageMask);
		}

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged {
#if DEBUG
			checked { GL.NamedBufferSubData((int)Handle, (nint)offset, sizeof(T) * data.Length, data); }
#else
			GL.NamedBufferSubData((int)Handle, (nint)offset, sizeof(T) * data.Length, data);
#endif
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			GL.DeleteBuffer((int)Handle);

			WasDestroyed = true;
		}
	}
}