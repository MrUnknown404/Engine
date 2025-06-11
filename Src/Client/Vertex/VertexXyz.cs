using System.Runtime.InteropServices;

namespace Engine3.Client.Vertex {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct VertexXyz : IVertex {
		public float X { get; init; }
		public float Y { get; init; }
		public float Z { get; init; }

		public VertexXyz(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}
	}
}