using System.Numerics;
using JetBrains.Annotations;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.OpenGL {
	[PublicAPI]
	public sealed class GLBuffer {
		public BufferHandle Handle { get; private set; }
		public bool WasFreed { get; private set; }

		public bool HasHandle => Handle.Handle != 0;

		public void GenBuffer() {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (HasHandle) { throw new Exception(); } // TODO handle/exception

			Handle = GLH.CreateBuffer();
		}

		public void NamedBufferData<T>(ReadOnlySpan<T> data, VertexBufferObjectUsage usage) where T : unmanaged, INumber<T> {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!HasHandle) { throw new Exception(); } // TODO handle/exception

			GLH.NamedBufferData(Handle, data, usage);
		}

		public void NamedBufferStorage<T>(ReadOnlySpan<T> data, BufferStorageMask mask) where T : unmanaged, INumber<T> {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!HasHandle) { throw new Exception(); } // TODO handle/exception

			GLH.NamedBufferStorage(Handle, data, mask);
		}

		public void NamedBufferSubData<T>(ReadOnlySpan<T> data, int offset = 0) where T : unmanaged, INumber<T> {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!HasHandle) { throw new Exception(); } // TODO handle/exception

			GLH.NamedBufferSubData(Handle, data, offset);
		}

		public void Free(bool deleteBuffer = true) {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!HasHandle) { throw new Exception(); } // TODO handle/exception

			if (deleteBuffer) { GLH.DeleteBuffer(Handle); }

			Handle = new();
			WasFreed = true;
		}
	}
}