using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL.Model;

namespace USharpLibs.Engine.Client.GL.Shapes2D {
	[PublicAPI]
	public static class Hexagons { // TODO document this
		public const float Sqrt3 = 1.73205080757f; // MathF.Sqrt(3)
		public const float HalfSqrt3 = Sqrt3 / 2f;

		public static Mesh HollowFlatTop_Flipped(float x, float y, float size, float thickness, float z) {
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x - r,   y,       z, 0, 0,
					x - r2,  y + r3,  z, 0, 0,
					x - r2,  y - r3,  z, 0, 0,
					x + r2,  y + r3,  z, 0, 0,
					x + r2,  y - r3,  z, 0, 0,
					x + r,   y,       z, 0, 0,

					x - ir,  y,       z, 0, 0,
					x - ir2, y + ir3, z, 0, 0,
					x - ir2, y - ir3, z, 0, 0,
					x + ir2, y + ir3, z, 0, 0,
					x + ir2, y - ir3, z, 0, 0,
					x + ir,  y,       z, 0, 0,
			}, new uint[] { 1, 3, 7, 3, 9, 7, 0, 1, 6, 1, 7, 6, 2, 0, 8, 0, 6, 8, 4, 2, 10, 2, 8, 10, 5, 4, 11, 4, 10, 11, 3, 5, 9, 5, 11, 9, });
			//@formatter:on
		}

		public static Mesh HollowPointyTop_Flipped(float x, float y, float size, float thickness, float z) { // TODO figure out how to texture this
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x,       y + r,   z, 0, 0,
					x + r3,  y + r2,  z, 0, 0,
					x - r3,  y + r2,  z, 0, 0,
					x + r3,  y - r2,  z, 0, 0,
					x - r3,  y - r2,  z, 0, 0,
					x,       y - r,   z, 0, 0,

					x,       y + ir,  z, 0, 0,
					x + ir3, y + ir2, z, 0, 0,
					x - ir3, y + ir2, z, 0, 0,
					x + ir3, y - ir2, z, 0, 0,
					x - ir3, y - ir2, z, 0, 0,
					x,       y - ir,  z, 0, 0,
			}, new uint[] { 1, 3, 7, 3, 9, 7, 0, 1, 6, 1, 7, 6, 2, 0, 8, 0, 6, 8, 4, 2, 10, 2, 8, 10, 5, 4, 11, 4, 10, 11, 3, 5, 9, 5, 11, 9, });
			//@formatter:on
		}

		public static Mesh FlatTop_Flipped(float x, float y, float size, float z) { // TODO figure out how to texture this
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x - r,  y,      z, 0, 0,
					x - r2, y + r3, z, 0, 0,
					x - r2, y - r3, z, 0, 0,
					x + r2, y + r3, z, 0, 0,
					x + r2, y - r3, z, 0, 0,
					x + r,  y,      z, 0, 0,
			}, new uint[] { 0, 1, 2, 1, 4, 2, 1, 3, 4, 3, 5, 4, });
			//@formatter:on
		}

		public static Mesh PointyTop_Flipped(float x, float y, float size, float z) { // TODO figure out how to texture this
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x,      y + r,  z, 0, 0,
					x + r3, y + r2, z, 0, 0,
					x - r3, y + r2, z, 0, 0,
					x + r3, y - r2, z, 0, 0,
					x - r3, y - r2, z, 0, 0,
					x,      y - r,  z, 0, 0,
			}, new uint[] { 0, 1, 2, 1, 4, 2, 1, 3, 4, 3, 5, 4, });
			//@formatter:on
		}

		public static Mesh HollowFlatTop(float x, float y, float size, float thickness, float z) { // TODO figure out how to texture this
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x - r,   y,       z, 0, 0,
					x - r2,  y - r3,  z, 0, 0,
					x - r2,  y + r3,  z, 0, 0,
					x + r2,  y - r3,  z, 0, 0,
					x + r2,  y + r3,  z, 0, 0,
					x + r,   y,       z, 0, 0,

					x - ir,  y,       z, 0, 0,
					x - ir2, y - ir3, z, 0, 0,
					x - ir2, y + ir3, z, 0, 0,
					x + ir2, y - ir3, z, 0, 0,
					x + ir2, y + ir3, z, 0, 0,
					x + ir,  y,       z, 0, 0,
			}, new uint[] { 1, 3, 7, 3, 9, 7, 0, 1, 6, 1, 7, 6, 2, 0, 8, 0, 6, 8, 4, 2, 10, 2, 8, 10, 5, 4, 11, 4, 10, 11, 3, 5, 9, 5, 11, 9, });
			//@formatter:on
		}

		public static Mesh HollowPointyTop(float x, float y, float size, float thickness, float z) { // TODO figure out how to texture this
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x,       y - r,   z, 0, 0,
					x + r3,  y - r2,  z, 0, 0,
					x - r3,  y - r2,  z, 0, 0,
					x + r3,  y + r2,  z, 0, 0,
					x - r3,  y + r2,  z, 0, 0,
					x,       y + r,   z, 0, 0,

					x,       y - ir,  z, 0, 0,
					x + ir3, y - ir2, z, 0, 0,
					x - ir3, y - ir2, z, 0, 0,
					x + ir3, y + ir2, z, 0, 0,
					x - ir3, y + ir2, z, 0, 0,
					x,       y + ir,  z, 0, 0,
			}, new uint[] { 1, 3, 7, 3, 9, 7, 0, 1, 6, 1, 7, 6, 2, 0, 8, 0, 6, 8, 4, 2, 10, 2, 8, 10, 5, 4, 11, 4, 10, 11, 3, 5, 9, 5, 11, 9, });
			//@formatter:on
		}

		public static Mesh FlatTop(float x, float y, float size, float z) { // TODO figure out how to texture this
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x - r,  y,      z, 0, 0,
					x - r2, y - r3, z, 0, 0,
					x - r2, y + r3, z, 0, 0,
					x + r2, y - r3, z, 0, 0,
					x + r2, y + r3, z, 0, 0,
					x + r,  y,      z, 0, 0,
			}, new uint[] { 0, 1, 2, 1, 4, 2, 1, 3, 4, 3, 5, 4, });
			//@formatter:on
		}

		public static Mesh PointyTop(float x, float y, float size, float z) { // TODO figure out how to texture this
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * HalfSqrt3;

			//@formatter:off
			return new(new[] {
					x,      y - r,  z, 0, 0,
					x + r3, y - r2, z, 0, 0,
					x - r3, y - r2, z, 0, 0,
					x + r3, y + r2, z, 0, 0,
					x - r3, y + r2, z, 0, 0,
					x,      y + r,  z, 0, 0,
			}, new uint[] { 0, 1, 2, 1, 4, 2, 1, 3, 4, 3, 5, 4, });
			//@formatter:on
		}
	}
}