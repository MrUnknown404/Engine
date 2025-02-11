using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Client.Models.Vertex;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models.Meshes {
	public class ImmutableInterleavedMesh<TVertex> : ImmutableMesh where TVertex : struct, IInterleavedVertex {
		private TVertex[] Vertices { get; }

		public ImmutableInterleavedMesh(uint[] indices, TVertex[] vertices) : base(indices) {
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
				VertexLayout layout = TVertex.VertexLayout[attribIndex];
				BindVertexAttrib(attribIndex, layout, TVertex.SizeInBytes, ref offset);
			}
		}
	}
}