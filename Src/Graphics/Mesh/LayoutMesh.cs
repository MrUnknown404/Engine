using System.Runtime.InteropServices;
using Engine3.Graphics.Vertex;
using JetBrains.Annotations;

namespace Engine3.Graphics.Mesh {
	[PublicAPI]
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