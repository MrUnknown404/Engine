using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models {
	public abstract class InterleavedMesh : Mesh<uint[]> {
		protected InterleavedMesh(uint[] indices) : base(indices) { }
	}

	public class InterleavedMesh<TVertex> : InterleavedMesh where TVertex : struct, IVertexLayout {
		private TVertex[] Vertices { get; }

		public InterleavedMesh(uint[] indices, TVertex[] vertices) : base(indices) {
			_ = MeshErrorHandler.Assert(vertices.Length == 0, static () => new(MeshErrorHandler.Reason.EmptyVertexArray));

			Vertices = vertices;
		}

		protected internal override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
			byte[] vertices = CollectVertexData(Vertices);

			BindArrayBuffer(vbo, vertices, vertices.Length, bufferHint);
			BindVertexAttribs();
			BindIndices(ebo, bufferHint);
		}

		private static void BindVertexAttribs() {
			byte offset = 0;
			for (uint attribIndex = 0; attribIndex < TVertex.VertexLayout.Length; attribIndex++) {
				VertexAttribLayout layout = TVertex.VertexLayout[attribIndex];
				BindVertexAttrib(attribIndex, layout, TVertex.SizeInBytes, ref offset);
			}
		}
	}
}