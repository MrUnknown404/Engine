using System.Numerics;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client.Model {
	[PublicAPI]
	public class GLBuffer {
		public uint Handle {
			get;
			internal set {
				if (HasHandle) { throw new GLBufferException(GLBufferException.Reason.HasHandle); }
				field = value;
			}
		}

		public bool WasFreed { get; private set; }
		public bool HasHandle => Handle != 0;

		private int firstSize;
		private BufferStorageType bufferStorageType = BufferStorageType.Unknown;

		internal void BindBuffer<T>(T[] data, BufferStorageFlags flags) where T : unmanaged, INumber<T> {
			if (WasFreed) { throw new GLBufferException(GLBufferException.Reason.WasFreed); }
			if (bufferStorageType == BufferStorageType.Resizable) { throw new GLBufferException(GLBufferException.Reason.SwitchedStorageType); }

			int size;
			unsafe { size = data.Length * sizeof(T); }

			if (bufferStorageType == BufferStorageType.Unknown) { firstSize = data.Length; }
			if (firstSize != data.Length) { throw new GLBufferException(GLBufferException.Reason.BufferResizeError); }

			GL.NamedBufferStorage(Handle, size, data, flags);
			bufferStorageType = BufferStorageType.Constant;
		}

		internal void BindBuffer<T>(T[] data, BufferUsageHint hint) where T : unmanaged, INumber<T> {
			if (WasFreed) { throw new GLBufferException(GLBufferException.Reason.WasFreed); }
			if (bufferStorageType == BufferStorageType.Constant) { throw new GLBufferException(GLBufferException.Reason.SwitchedStorageType); }

			int size;
			unsafe { size = data.Length * sizeof(T); }

			if (bufferStorageType == BufferStorageType.Unknown) { firstSize = size; }

			GL.NamedBufferData(Handle, size, data, hint);
			bufferStorageType = BufferStorageType.Resizable;
		}

		public void Free(bool deleteBuffers = true) {
			if (WasFreed) { return; }

			if (HasHandle) {
				if (deleteBuffers) { GL.DeleteBuffer(Handle); }
				Handle = 0;
			}

			WasFreed = true;
		}

		private enum BufferStorageType : byte {
			Unknown = 0,
			Resizable,
			Constant,
		}
	}
}