using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;

namespace USharpLibs.Engine2.Client.Models.Meshes {
	public class MutableInterleavedMesh<TVertex> : MutableMesh where TVertex : struct, IInterleavedVertex {
		private List<TVertex> Vertices { get; } = new();

		protected internal override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			byte[] vertices = CollectVertexData(Vertices);

			BindArrayBuffer(vbo, vertices, vertices.Length, bufferHint);
			BindVertexAttribs();
			BindIndices(ebo, bufferHint);
		}

		private static void BindVertexAttribs() {
			byte offset = 0;
			for (uint attribIndex = 0; attribIndex < TVertex.VertexLayout.Length; attribIndex++) {
				VertexLayout layout = TVertex.VertexLayout[attribIndex];
				BindVertexAttrib(attribIndex, layout, TVertex.SizeInBytes, ref offset);
			}
		}

		// TODO mutation methods & check!
	}
}