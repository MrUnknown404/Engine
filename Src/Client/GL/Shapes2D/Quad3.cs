using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL.Models;
using USharpLibs.Engine.Client.GL.Models.Vertex;

namespace USharpLibs.Engine.Client.GL.Shapes2D {
	[PublicAPI]
	public static class Quad3 {
		[MustUseReturnValue] public static Mesh<Vertex3> WH(float w, float h, float z) => XYWH(0, 0, z, w, h);
		[MustUseReturnValue] public static Mesh<Vertex3> XYWH(float x, float y, float z, float w, float h) => Raw(x, y + h, x + w, y, z);

		//@formatter:off
		[MustUseReturnValue]
		public static Mesh<Vertex3> Raw(float x0, float y0, float x1, float y1, float z) => Mesh<Vertex3>.Local(new Vertex3[] {
				new(x0, y0, z),
				new(x0, y1, z),
				new(x1, y0, z),
				new(x1, y1, z),
		}, new uint[] { 0, 2, 1, 2, 3, 1, });
		// @formatter:on
	}
}