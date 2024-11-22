using USharpLibs.Engine2.Client.Models.Vertex;

namespace USharpLibs.Engine2.Client.Models {
	[PublicAPI]
	public sealed class Mesh<TVertex> where TVertex : IVertex {
		public TVertex[] Vertices { get; }
		public uint[] Indices { get; }

		public Mesh(TVertex[] vertices, uint[] indices) {
			if (vertices.Length == 0) { throw new ArgumentException("Vertex array cannot be empty."); }
			if (indices.Length == 0) { throw new ArgumentException("Index array cannot be empty."); }
			if (indices.Length % 3 != 0) { throw new ArgumentException($"Index array is not divisible by 3. Was {indices.Length}."); }

			Vertices = vertices;
			Indices = indices;
		}

		[MustUseReturnValue]
		public float[] CollectVertices() {
			float[] vertices = new float[Vertices.Length * TVertex.Length];
			for (int i = 0; i < Vertices.Length; i++) { Vertices[i].Collect(ref vertices, i * TVertex.Length, TVertex.Length); }
			return vertices;
		}
	}
}