using Engine3.Client.Model.Mesh.Vertex;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client.Model {
	// TODO modify this to take in a GLBuffer[] in the constructor. then methods for setting up said buffers

	[PublicAPI]
	public class VertexArrayObject {
		private uint vao;

		public GLBuffer[] Buffers { get; }

		public uint Vao { get => vao; private set => vao = value; }
		public bool WasFreed { get; private set; }
		public bool WereAttributesBound { get; private set; }

		public bool HasHandle => Vao != 0;
		public int IndexCount { get; private set; }

		public VertexArrayObject(byte bufferCount) {
			// TODO check bufferCount

			Buffers = new GLBuffer[bufferCount];

			for (int i = 0; i < Buffers.Length; i++) { Buffers[i] = new(); }
		}

		public void GenBuffers() {
			if (WasFreed) { throw new VaoException(VaoException.Reason.WasFreed); }
			if (HasHandle) { throw new VaoException(VaoException.Reason.HasHandle); }

			GL.CreateVertexArrays(1, out vao);

			uint[] buffers = new uint[Buffers.Length];
			GL.CreateBuffers(buffers.Length, buffers);

			for (int i = 0; i < buffers.Length; i++) { Buffers[i].Handle = buffers[i]; }
		}

		public void BindVertexBuffer(byte bufferIndex, byte[] data, VertexLayout layout, BufferUsageHint hint) {
			if (WasFreed) { throw new VaoException(VaoException.Reason.WasFreed); }
			if (!HasHandle) { throw new VaoException(VaoException.Reason.NoHandle); }

			bufferIndex++;
			GLBuffer buffer = Buffers[bufferIndex];
			buffer.BindBuffer(data, hint);
			GL.VertexArrayVertexBuffer(Vao, 0, buffer.Handle, 0, layout.Size);
		}

		public void BindIndexBuffer(uint[] data, BufferUsageHint hint) {
			GLBuffer buffer = Buffers[0];
			buffer.BindBuffer(data, hint);
			GL.VertexArrayElementBuffer(Vao, buffer.Handle);
			IndexCount = data.Length;
		}

		public void BindVertexBuffer(byte bufferIndex, byte[] data, VertexLayout layout, BufferStorageFlags flags) {
			if (WasFreed) { throw new VaoException(VaoException.Reason.WasFreed); }
			if (!HasHandle) { throw new VaoException(VaoException.Reason.NoHandle); }

			bufferIndex++;
			GLBuffer buffer = Buffers[bufferIndex];
			buffer.BindBuffer(data, flags);
			GL.VertexArrayVertexBuffer(Vao, 0, buffer.Handle, 0, layout.Size);
		}

		public void BindIndexBuffer(uint[] data, BufferStorageFlags flags) {
			GLBuffer buffer = Buffers[0];
			buffer.BindBuffer(data, flags);
			GL.VertexArrayElementBuffer(Vao, buffer.Handle);
		}

		public void BindVertexAttributes(VertexLayout vertexLayout) {
			if (WasFreed) { throw new VaoException(VaoException.Reason.WasFreed); }
			if (!HasHandle) { throw new VaoException(VaoException.Reason.NoHandle); }

			VertexAttribute[] layout = vertexLayout.Layout;
			uint offset = 0;

			for (uint i = 0; i < layout.Length; i++) {
				VertexAttribute attribute = layout[i];
				GL.EnableVertexArrayAttrib(Vao, i);
				GL.VertexArrayAttribFormat(vao, i, attribute.ElementCount, attribute.VertexAttribType, false, offset);
				GL.VertexArrayAttribBinding(Vao, i, 0);
				offset += vertexLayout.Size;
			}

			WereAttributesBound = true;
		}

		public void Free() {
			if (WasFreed) { return; }

			if (HasHandle) {
				uint[] buffers = Buffers.Select(static b => b.Handle).ToArray();

				GL.DeleteBuffers(buffers.Length, buffers);
				foreach (GLBuffer buffer in Buffers) { buffer.Free(false); } // OpenGL buffers deleted above

				GL.DeleteVertexArray(Vao);
				Vao = 0;
			}

			WasFreed = true;
		}
	}
}