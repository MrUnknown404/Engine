using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.Vertex {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct VertexUv : IVertexAttribute {
		public static VertexAttributeFormat[] VertexFormat { get; } = [ new(VertexAttribType.Float, 2), ];

		public float U { get; init; }
		public float V { get; init; }

		public VertexUv(float u, float v) {
			U = u;
			V = v;
		}
	}
}