using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL.Models.Vertex;

namespace USharpLibs.Engine.Client.GL.Models {
	[PublicAPI]
	public sealed class Mesh<T> where T : IVertex {
		public T[] Vertices { get; }
		public uint[] Indices { get; }

		/// <summary> Whether or not to automatically offset indices in NewModel#<see cref="Model{TVertex,TCollection}.ProcessMeshesIntoIndexArray"/> </summary>
		public bool AreIndicesGlobal { get; }

		// Decided to make this require a factory instead since it makes it a little easier to read
		private Mesh(T[] vertices, uint[] indices, bool areIndicesGlobal) {
			if (vertices.Length == 0) { throw new ArgumentException("Vertex array cannot be empty"); }
			if (indices.Length == 0) { throw new ArgumentException("Index array cannot be empty"); }
			if (indices.Length % 3 != 0) { throw new ArgumentException($"Index array is not divisible by 3. Was {indices.Length}"); }

			Vertices = vertices;
			Indices = indices;
			AreIndicesGlobal = areIndicesGlobal;
		}

		[MustUseReturnValue] public static Mesh<T> Local(T[] vertices, uint[] indices) => new(vertices, indices, false);
		[MustUseReturnValue] public static Mesh<T> Global(T[] vertices, uint[] indices) => new(vertices, indices, true);
	}
}