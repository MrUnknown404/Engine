using JetBrains.Annotations;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	[PublicAPI]
	public unsafe class OpenGLBuffer : IBufferObject, IEquatable<OpenGLBuffer> {
		public BufferHandle Handle { get; }
		public ulong BufferSize { get; } // Closest to GLsizeiptr is nint? - https://wikis.khronos.org/opengl/OpenGL_Type

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		internal OpenGLBuffer(string debugName, ulong bufferSize, BufferStorageMask bufferStorageMask) {
			DebugName = debugName;
			BufferSize = bufferSize;
			Handle = new(GL.CreateBuffer());

#if DEBUG
			checked { GL.NamedBufferStorage((int)Handle, (nint)BufferSize, IntPtr.Zero, bufferStorageMask); }
#else
			GL.NamedBufferStorage((int)Handle, (nint)BufferSize, IntPtr.Zero, bufferStorageMask);
#endif

			INamedGraphicsResource.PrintNameWithHandle(this, Handle.Handle);
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

		public bool Equals(OpenGLBuffer? other) => other != null && Handle == other.Handle;
		public override bool Equals(object? obj) => obj is OpenGLBuffer buffer && Equals(buffer);

		public override int GetHashCode() => Handle.GetHashCode();

		public static bool operator ==(OpenGLBuffer? left, OpenGLBuffer? right) => Equals(left, right);
		public static bool operator !=(OpenGLBuffer? left, OpenGLBuffer? right) => !Equals(left, right);
	}
}