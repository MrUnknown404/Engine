using System.Runtime.InteropServices;

namespace Engine3.Client.Vertex {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct VertexXyzUv : IVertex {
		public VertexXyz Xyz { get; init; } = new();
		public VertexUv Uv { get; init; } = new();

		public VertexXyzUv(in VertexXyz xyz, in VertexUv uv) {
			Xyz = xyz;
			Uv = uv;
		}

		public VertexXyzUv(float x, float y, float z, float u, float v) : this(new(x, y, z), new(u, v)) { }
	}
}