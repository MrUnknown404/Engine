using JetBrains.Annotations;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.OpenGL.Objects {
	[PublicAPI]
	public unsafe class GlBufferObject : IGlBufferObject { // Closest to GLsizeiptr is nint? - https://wikis.khronos.org/opengl/OpenGL_Type
		public BufferHandle Handle { get; }
		public nint BufferSize { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		public GlBufferObject(string debugName, nint bufferSize, BufferStorageMask bufferStorageMask) {
			DebugName = debugName;
			BufferSize = bufferSize;
			Handle = new(GL.CreateBuffer());

			GL.NamedBufferStorage((int)Handle, BufferSize, IntPtr.Zero, bufferStorageMask);
		}

		public void Copy<T>(ReadOnlySpan<T> data) where T : unmanaged => Copy(data, 0);
		public void Copy<T>(ReadOnlySpan<T> data, nint offset) where T : unmanaged => GL.NamedBufferSubData((int)Handle, offset, sizeof(T) * data.Length, data);

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			GL.DeleteBuffer((int)Handle);

			WasDestroyed = true;
		}
	}
}