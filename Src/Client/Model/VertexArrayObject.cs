using Engine3.Client.Model.Mesh.Vertex;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client.Model {
	public class VertexArrayObject {
		private uint vao;

		public uint[] Buffers { get; }

		public uint Vao { get => vao; private set => vao = value; }
		public int IndexCount { get; private set; }
		public bool WasFreed { get; private set; }
		public bool WereAttributesBound { get; private set; }

		public bool WereBuffersCreated { get; private set; }

		public VertexArrayObject(byte bufferCount) => Buffers = new uint[bufferCount];

		public void GenBuffers() {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (WereBuffersCreated) { throw new Exception(); } // TODO handle/exception

			GL.CreateVertexArrays(1, out vao);
			GL.CreateBuffers(Buffers.Length, Buffers);

			WereBuffersCreated = true;
		}

		public void BindVertexBuffer(uint buffer, byte[] data, BufferUsageHint hint, byte vertexSize) {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!WereBuffersCreated) { throw new Exception(); } // TODO handle/exception

			GL.VertexArrayVertexBuffer(Vao, 0, buffer, 0, vertexSize);
			GL.NamedBufferData(buffer, data.Length, data, hint); // TODO support NamedBufferStorage
		}

		public void BindIndexBuffer(uint buffer, uint[] data, BufferUsageHint hint) {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!WereBuffersCreated) { throw new Exception(); } // TODO handle/exception

			GL.VertexArrayElementBuffer(Vao, buffer);
			GL.NamedBufferData(buffer, data.Length * sizeof(uint), data, hint); // TODO support NamedBufferStorage
			IndexCount = data.Length;
		}

		public void BindVertexAttributes(VertexLayout vertexLayout) {
			if (WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!WereBuffersCreated) { throw new Exception(); } // TODO handle/exception

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

			if (WereBuffersCreated) {
				GL.DeleteBuffers(Buffers.Length, Buffers);
				for (int i = 0; i < Buffers.Length; i++) { Buffers[i] = 0; }

				GL.DeleteVertexArray(Vao);
				Vao = 0;
			}

			WasFreed = true;
		}
	}
}