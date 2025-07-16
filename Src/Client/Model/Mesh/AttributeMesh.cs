using System.Runtime.InteropServices;
using Engine3.Client.Model.Mesh.Vertex;
using JetBrains.Annotations;

namespace Engine3.Client.Model.Mesh {
	[PublicAPI]
	public class AttributeMesh<T0> where T0 : unmanaged, IVertexAttribute {
		public T0[] Vertices0 { get; }
		public uint[] Indices { get; }

		public AttributeMesh(T0[] vertices0, uint[] indices) {
			// TODO check

			Vertices0 = vertices0;
			Indices = indices;
		}

		public ReadOnlySpan<byte> VerticesAsBytes() => Vertices0AsBytes();
		public ReadOnlySpan<byte> Vertices0AsBytes() => MemoryMarshal.AsBytes(Vertices0.AsSpan());
	}

	[PublicAPI]
	public class AttributeMesh<T0, T1> where T0 : unmanaged, IVertexAttribute where T1 : unmanaged, IVertexAttribute {
		public T0[] Vertices0 { get; }
		public T1[] Vertices1 { get; }
		public uint[] Indices { get; }

		public AttributeMesh(T0[] vertices0, T1[] vertices1, uint[] indices) {
			// TODO check

			Vertices0 = vertices0;
			Vertices1 = vertices1;
			Indices = indices;
		}

		public byte[] VerticesAsBytes() {
			List<byte> b = new();
			b.AddRange(MemoryMarshal.AsBytes(Vertices0.AsSpan()));
			b.AddRange(MemoryMarshal.AsBytes(Vertices1.AsSpan()));
			return b.ToArray();
		}

		public ReadOnlySpan<byte> Vertices0AsBytes() => MemoryMarshal.AsBytes(Vertices0.AsSpan());
		public ReadOnlySpan<byte> Vertices1AsBytes() => MemoryMarshal.AsBytes(Vertices1.AsSpan());
	}
}