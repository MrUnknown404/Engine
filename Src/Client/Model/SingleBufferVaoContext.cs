using Engine3.Client.Model.Mesh.Vertex;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client.Model {
	public class SingleBufferVaoContext { // TODO think about this class. do i want it?
		internal VertexArrayObject? Vao { get; set; }
		private int eboCount;

		public void BindVertexAttributes(VertexLayout[] layouts) {
			if (Vao == null) { throw new Exception(); } // TODO handle/exception
			if (Vao.WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!Vao.WasVaoSet) { throw new Exception(); } // TODO handle/exception
			if (!Vao.WasVboSet) { throw new Exception(); } // TODO handle/exception

			int offset = 0;

			for (int i = 0; i < layouts.Length; i++) {
				VertexLayout layout = layouts[i];
				GL.EnableVertexAttribArray(i);
				GL.VertexAttribPointer(i, layout.ElementCount, layout.VertexAttribType, false, layout.ElementCount * layout.ElementByteSize, offset);
				offset += layout.ElementCount * layout.ElementByteSize;
			}

			Vao.WereAttributesBound = true;
		}

		public void BindBuffers(byte[] vbo, uint[] ebo, BufferUsageHint hint) {
			if (Vao == null) { throw new Exception(); } // TODO handle/exception
			if (Vao.WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!Vao.WasVaoSet) { throw new Exception(); } // TODO handle/exception

			GLH.SetBuffer(Vao.Vbo, vbo, BufferTarget.ArrayBuffer, hint);
			GLH.SetBuffer(Vao.Ebo, ebo, BufferTarget.ElementArrayBuffer, hint);
			eboCount = ebo.Length;
		}

		public void Draw() {
			if (Vao == null) { throw new Exception(); } // TODO handle/exception
			if (Vao.WasFreed) { throw new Exception(); } // TODO handle/exception
			if (!Vao.WasVaoSet) { throw new Exception(); } // TODO handle/exception
			if (!Vao.WereAttributesBound) { throw new Exception(); } // TODO handle/exception
			if (eboCount == 0) { throw new Exception(); } // TODO handle/exception

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, Vao.Ebo);
			GL.DrawElements(PrimitiveType.Triangles, eboCount, DrawElementsType.UnsignedInt, 0);
		}
	}
}