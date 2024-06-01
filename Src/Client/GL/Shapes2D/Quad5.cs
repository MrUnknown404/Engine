using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL.Models;
using USharpLibs.Engine.Client.GL.Models.Vertex;

namespace USharpLibs.Engine.Client.GL.Shapes2D {
	[PublicAPI]
	public static class Quad5 {
		[MustUseReturnValue] public static Mesh<Vertex5> WH(float w, float h, float z) => XYZWH(0, 0, z, w, h, 0, 0, 1, 1);
		[MustUseReturnValue] public static Mesh<Vertex5> WH(float w, float h, float z, float u0, float v0, float u1, float v1) => XYZWH(0, 0, z, w, h, u0, v0, u1, v1);

		[MustUseReturnValue] public static Mesh<Vertex5> XYZWH(float x, float y, float z, float w, float h) => XYZWH(x, y, z, w, h, 0, 0, 1, 1);
		[MustUseReturnValue] public static Mesh<Vertex5> XYZWH(float x, float y, float z, float w, float h, float u0, float v0, float u1, float v1) => Raw(x, y + h, x + w, y, z, u0, v0, u1, v1);

		// @formatter:off
		[MustUseReturnValue]
		public static Mesh<Vertex5> Raw(float x0, float y0, float x1, float y1, float z, float u0, float v0, float u1, float v1) => Mesh<Vertex5>.Local(new Vertex5[] {
				new(x0, y0, z, u0, v0),
				new(x0, y1, z, u0, v1),
				new(x1, y0, z, u1, v0),
				new(x1, y1, z, u1, v1),
		}, new uint[] { 0, 2, 1, 2, 3, 1, });
		// @formatter:on
	}
}