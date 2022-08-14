using USharpLibs.Common.Utils;
using USharpLibs.Engine.Utils;

namespace USharpLibs.Engine.Client.GL {
	public readonly struct Shape {
		public readonly float[] Vertices;
		public Shape(float[] vertices) => Vertices = vertices;
	}

	public static class Cubes {
		private static readonly Direction[] All = EnumUtils.Values<Direction>();

		public static Shape S(float s) => Cube(0, 0, 0, s, s, s, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape XYZS(float x, float y, float z, float s) => Cube(x, y, z, x + s, y + s, z + s, 0, 0, 1, 0, 0, 1, 1, 1);

		public static Shape WHD(float w, float h, float d) => Cube(0, 0, 0, w, h, d, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape WHD(float w, float h, float d, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => Cube(0, 0, 0, w, h, d, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3);

		public static Shape XYZWHD(float x, float y, float z, float w, float h, float d) => Cube(x, y, z, x + w, y + h, d, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape XYZWHD(float x, float y, float z, float w, float h, float d, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => Cube(x, y, z, w, h, d, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3);

		public static Shape XYZ(float x0, float y0, float z0, float x1, float y1, float z1) => Cube(x0, y0, z0, x1, y1, z1, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape XYZ(float x0, float y0, float z0, float x1, float y1, float z1, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => Cube(x0, y0, z0, x1, y1, z1, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3);

		public static Shape FaceShape(Direction face, float x, float y, float z, float size) => FaceShape(face, x, y, z, size, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape FaceShape(Direction face, float x, float y, float z, float size, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => FaceShapeXYZ(face, x, y, z, x + size, y + size, z + size, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3);

		public static Shape FaceShapeXYZ(Direction face, float x0, float y0, float z0, float x1, float y1, float z1, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => new(FaceVertsXYZ(face, x0, y0, z0, x1, y1, z1, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3));
		public static float[] FaceVertsXYZ(Direction face, float x0, float y0, float z0, float x1, float y1, float z1, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => face switch {
			Direction.North => new[] {
				x0, y1, z0, tx3, ty3,
				x1, y1, z0, tx2, ty2,
				x0, y0, z0, tx1, ty1,
				x1, y0, z0, tx0, ty0,
				x0, y0, z0, tx1, ty1,
				x1, y1, z0, tx2, ty2,
			},
			Direction.South => new[] {
				x0, y0, z1, tx0, ty0,
				x1, y1, z1, tx3, ty3,
				x0, y1, z1, tx2, ty2,
				x1, y1, z1, tx3, ty3,
				x0, y0, z1, tx0, ty0,
				x1, y0, z1, tx1, ty1,
			},
			Direction.East => new[] {
				x1, y1, z0, tx3, ty3,
				x1, y1, z1, tx2, ty2,
				x1, y0, z0, tx1, ty1,
				x1, y0, z1, tx0, ty0,
				x1, y0, z0, tx1, ty1,
				x1, y1, z1, tx2, ty2,
			},
			Direction.West => new[] {
				x0, y0, z0, tx0, ty0,
				x0, y1, z1, tx3, ty3,
				x0, y1, z0, tx2, ty2,
				x0, y1, z1, tx3, ty3,
				x0, y0, z0, tx0, ty0,
				x0, y0, z1, tx1, ty1,
			},
			Direction.Up => new[] {
				x0, y1, z1, tx0, ty0,
				x1, y1, z1, tx1, ty1,
				x0, y1, z0, tx2, ty2,
				x1, y1, z0, tx3, ty3,
				x0, y1, z0, tx2, ty2,
				x1, y1, z1, tx1, ty1,
			},
			Direction.Down => new[] {
				x0, y0, z0, tx3, ty3,
				x1, y0, z1, tx0, ty0,
				x0, y0, z1, tx1, ty1,
				x1, y0, z1, tx0, ty0,
				x0, y0, z0, tx3, ty3,
				x1, y0, z0, tx2, ty2,
			},
			_ => throw new NotImplementedException(),
		};

		private static Shape Cube(float x0, float y0, float z0, float x1, float y1, float z1, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) {
			List<float> verts = new(All.Length * 6 * 5);
			foreach (Direction side in All) { verts.AddRange(FaceVertsXYZ(side, x0, y0, z0, x1, y1, z1, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3)); }
			return new(verts.ToArray());
		}
	}

	public static class Quads {
		public static Shape WH(float w, float h, float z = 0) => Quad(0, 0, z, w, h, z, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape WH(float w, float h, float z, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => Quad(0, 0, z, w, h, z, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3);

		public static Shape XYWH(float x, float y, float w, float h, float z = 0) => Quad(x, y, z, x + w, y + h, z, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape XYWH(float x, float y, float w, float h, float z, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => Quad(x, y, z, x + w, y + h, z, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3);

		public static Shape XYZ(float x0, float y0, float z0, float x1, float y1, float z1) => Quad(x0, y0, z0, x1, y1, z1, 0, 0, 1, 0, 0, 1, 1, 1);
		public static Shape XYZ(float x0, float y0, float z0, float x1, float y1, float z1, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) => Quad(x0, y0, z0, x1, y1, z1, tx0, ty0, tx1, ty1, tx2, ty2, tx3, ty3);

		private static Shape Quad(float x0, float y0, float z0, float x1, float y1, float z1, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2, float tx3, float ty3) {
			float zd = (z0 - z1) / 2f;
			return new(new[] {
				x0, y0, z0 - zd, tx0, ty0,
				x1, y1, z0 - zd, tx3, ty3,
				x0, y1, z0, tx2, ty2,
				x1, y1, z1 + zd, tx3, ty3,
				x0, y0, z1 + zd, tx0, ty0,
				x1, y0, z1, tx1, ty1,
			});
		}
	}

	public static class Tris {
		public static Shape XYZ(float x0, float y0, float x1, float y1, float x2, float y2, float z = 0) => Tri(x0, y0, z, x1, y1, z, x2, y2, z, 0, 0, 0, 0, 0, 0);
		public static Shape XYZ(float x0, float y0, float x1, float y1, float x2, float y2, float z, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2) => Tri(x0, y0, z, x1, y1, z, x2, y2, z, tx0, ty0, tx1, ty1, tx2, ty2);

		public static Shape XYZ(float x0, float y0, float z0, float x1, float y1, float z1, float x2, float y2, float z2) => Tri(x0, y0, z0, x1, y1, z1, x2, y2, z2, 0, 0, 0, 0, 0, 0);
		public static Shape XYZ(float x0, float y0, float z0, float x1, float y1, float z1, float x2, float y2, float z2, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2) => Tri(x0, y0, z0, x1, y1, z1, x2, y2, z2, tx0, ty0, tx1, ty1, tx2, ty2);

		private static Shape Tri(float x0, float y0, float z0, float x1, float y1, float z1, float x2, float y2, float z2, float tx0, float ty0, float tx1, float ty1, float tx2, float ty2) => new(new[] {
			x0, y0, z0, tx0, ty0,
			x1, y1, z1, tx1, ty1,
			x2, y2, z2, tx2, ty2,
		});
	}
}