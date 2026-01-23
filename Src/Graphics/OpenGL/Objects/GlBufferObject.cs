using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.OpenGL.Objects {
	public unsafe class GlBufferObject : IBufferObject<nint> { // Closest to GLsizeiptr is nint? - https://wikis.khronos.org/opengl/OpenGL_Type
		public BufferHandle Handle { get; }
		public BufferStorageMask BufferStorageMask { get; }
		public nint BufferSize { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public GlBufferObject(string debugName, BufferStorageMask bufferStorageMask, nint bufferSize) {
			DebugName = debugName;
			Handle = new(GL.CreateBuffer());
			BufferStorageMask = bufferStorageMask;
			BufferSize = bufferSize;

			GL.NamedBufferStorage((int)Handle, BufferSize, IntPtr.Zero, BufferStorageMask);
		}

		public void Copy<T>(T[] data, nint offset = 0) where T : unmanaged => GL.NamedBufferSubData((int)Handle, offset, sizeof(T) * data.Length, data);

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			GL.DeleteBuffer((int)Handle);

			WasDestroyed = true;
		}
	}
}