using System.Runtime.InteropServices;

namespace Engine3.Client.Model.Mesh.Vertex {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct VertexUv : IVertexAttribute {
		public float U { get; init; }
		public float V { get; init; }

		public VertexUv(float u, float v) {
			U = u;
			V = v;
		}
	}
}