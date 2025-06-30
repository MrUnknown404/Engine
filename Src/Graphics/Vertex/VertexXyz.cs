using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.Vertex {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct VertexXyz : IVertexAttribute {
		public static VertexAttributeFormat[] VertexFormat { get; } = [ new(VertexAttribType.Float, 3), ];

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