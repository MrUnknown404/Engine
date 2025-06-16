using System.Runtime.InteropServices;
using Engine3.Client.Model.Mesh.Vertex;

namespace Engine3.Client.Model.Mesh {
	public class LayoutMesh<T> where T : unmanaged, IVertex {
		public T[] Vertices { get; }
		public uint[] Indices { get; }

		public LayoutMesh(T[] vertices, uint[] indices) {
			// TODO check

			Vertices = vertices;
			Indices = indices;
		}

		public ReadOnlySpan<byte> VerticesAsBytes() => MemoryMarshal.AsBytes(Vertices.AsSpan());
	}
}