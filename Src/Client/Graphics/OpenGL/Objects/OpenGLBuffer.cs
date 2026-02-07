using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	[PublicAPI]
	public sealed unsafe class OpenGLBuffer : NamedGraphicsResource<OpenGLBuffer, nint> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public BufferHandle BufferHandle { get; }
		public ulong BufferSize { get; } // Closest to GLsizeiptr is nint? - https://wikis.khronos.org/opengl/OpenGL_Type

		protected override nint Handle => BufferHandle.Handle;

		internal OpenGLBuffer(string debugName, ulong bufferSize, BufferStorageMask bufferStorageMask) : base(debugName) {
			BufferSize = bufferSize;
			BufferHandle = new(GL.CreateBuffer());

#if DEBUG
			checked { GL.NamedBufferStorage((int)BufferHandle, (nint)BufferSize, IntPtr.Zero, bufferStorageMask); }
#else
			GL.NamedBufferStorage((int)ShaderHandle, (nint)BufferSize, IntPtr.Zero, bufferStorageMask);
#endif

			PrintCreate();
		}

		public void Copy<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged {
#if DEBUG
			checked { GL.NamedBufferSubData((int)BufferHandle, (nint)offset, sizeof(T) * data.Length, data); }
#else
			GL.NamedBufferSubData((int)ShaderHandle, (nint)offset, sizeof(T) * data.Length, data);
#endif
		}

		public void Copy(void* data, ulong bufferSize, ulong offset = 0) {
#if DEBUG
			checked { GL.NamedBufferSubData((int)BufferHandle, (nint)offset, (nint)bufferSize, data); }
#else
			GL.NamedBufferSubData((int)ShaderHandle, (nint)offset, (nint)bufferSize, data);
#endif
		}

		protected override void Cleanup() => GL.DeleteBuffer((int)BufferHandle);
	}
}