using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.GL.Model {
	[PublicAPI]
	public class Mesh {
		public float[] Vertices { get; protected set; }
		public uint[] Indices { get; protected set; }

		public Mesh(float[] vertices, uint[] indices) {
			if (vertices.Length == 0) { throw new ArgumentException("Vertex array cannot be empty"); }
			if (indices.Length == 0) { throw new ArgumentException("Index array cannot be empty"); }
			if (vertices.Length % 5 != 0) { throw new ArgumentException($"Vertex array is not divisible by 5. Was {vertices.Length}"); }
			if (indices.Length % 3 != 0) { throw new ArgumentException($"Index array is not divisible by 3. Was {indices.Length}"); }

			Vertices = vertices;
			Indices = indices;
		}
	}
}