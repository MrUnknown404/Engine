using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models.Interleaved {
	public abstract class InterleavedMesh<TIndex, TCollection, TVertex> : BaseMesh<TIndex>, IInterleavedMesh where TIndex : IList<uint> where TCollection : IList<TVertex> where TVertex : struct, IInterleavedVertex {
		private TCollection Vertices { get; }

		internal InterleavedMesh(TIndex indices, TCollection vertices, bool allowEmptyData) : base(indices, allowEmptyData) {
			_ = MeshErrorHandler.Assert(!allowEmptyData && vertices.Count == 0, static () => new(MeshErrorHandler.Reason.EmptyVertexArray));

			Vertices = vertices;
		}

		public override void BindToBuffer(uint vbo, uint ebo, BufferUsageHint bufferHint) {
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