using JetBrains.Annotations;
using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client.GL.Models;
using USharpLibs.Engine.Utils;

namespace USharpLibs.Engine.Client.GL.Shapes2D {
	// TODO figure out how to texture this
	// TODO document this
	[PublicAPI]
	public static class Hexagons {
		public static Mesh HollowFlatTopSide(FlatHexagonDirection side, float size, float thickness, float z = 0) => HollowFlatTopSide(side, 0, 0, size, thickness, z);

		public static Mesh HollowFlatTopSide(FlatHexagonDirection side, float x, float y, float size, float thickness, float z = 0) {
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }
			float r = size / 2f, r2 = r / 2f, r3 = r * MathH.HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * MathH.HalfSqrt3;
			r3 = -r3;
			ir3 = -ir3;

			//@formatter:off
			float[] vertices = side switch {
					FlatHexagonDirection.North => new[] {
							x + r2,  y + r3,  z, 0, 0,
							x - r2,  y + r3,  z, 0, 0,
							x + ir2, y + ir3, z, 0, 0,
							x - ir2, y + ir3, z, 0, 0,
					},
					FlatHexagonDirection.NorthEast => new[] {
							x + r,   y,       z, 0, 0,
							x + r2,  y + r3,  z, 0, 0,
							x + ir,  y,       z, 0, 0,
							x + ir2, y + ir3, z, 0, 0,
					},
					FlatHexagonDirection.SouthEast => new[] {
							x + r2,  y - r3,  z, 0, 0,
							x + r,   y,       z, 0, 0,
							x + ir2, y - ir3, z, 0, 0,
							x + ir,  y,       z, 0, 0,
					},
					FlatHexagonDirection.South => new[] {
							x - r2,  y - r3,  z, 0, 0,
							x + r2,  y - r3,  z, 0, 0,
							x - ir2, y - ir3, z, 0, 0,
					 		x + ir2, y - ir3, z, 0, 0,
					},
					FlatHexagonDirection.SouthWest => new[] {
							x - r,   y,       z, 0, 0,
					 		x - r2,  y - r3,  z, 0, 0,
							x - ir,  y,       z, 0, 0,
							x - ir2, y - ir3, z, 0, 0,
					},
					FlatHexagonDirection.NorthWest => new[] {
							x - r2,  y + r3,  z, 0, 0,
							x - r,   y,       z, 0, 0,
							x - ir2, y + ir3, z, 0, 0,
							x - ir,  y,       z, 0, 0,
					},
					_ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
			};
			//@formatter:on

			return new(vertices, new uint[] { 0, 1, 2, 1, 3, 2, });
		}

		public static Mesh HollowPointyTopSide(PointyHexagonDirection side, float size, float thickness, float z = 0) => HollowPointyTopSide(side, 0, 0, size, thickness, z);

		public static Mesh HollowPointyTopSide(PointyHexagonDirection side, float x, float y, float size, float thickness, float z = 0) {
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }
			float r = size / 2f, r2 = r / 2f, r3 = r * MathH.HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * MathH.HalfSqrt3;
			r = -r;
			r2 = -r2;
			ir = -ir;
			ir2 = -ir2;

			//@formatter:off
			float[] vertices = side switch {
					PointyHexagonDirection.East => new[] {
							x + r3,  y - r2,  z, 0, 0,
							x + r3,  y + r2,  z, 0, 0,
							x + ir3, y - ir2, z, 0, 0,
							x + ir3, y + ir2, z, 0, 0,
					},
					PointyHexagonDirection.NorthEast => new[] {
							x + r3,  y + r2,  z, 0, 0,
							x,       y + r,   z, 0, 0,
							x + ir3, y + ir2, z, 0, 0,
							x,       y + ir,  z, 0, 0,
					},
					PointyHexagonDirection.NorthWest => new[] {
							x,       y + r,   z, 0, 0,
			 				x - r3,  y + r2,  z, 0, 0,
							x,       y + ir,  z, 0, 0,
							x - ir3, y + ir2, z, 0, 0,
					},
					PointyHexagonDirection.West => new[] {
							x - r3,  y + r2,  z, 0, 0,
							x - r3,  y - r2,  z, 0, 0,
							x - ir3, y + ir2, z, 0, 0,
							x - ir3, y - ir2, z, 0, 0,
					},
					PointyHexagonDirection.SouthWest => new[] {
							x - r3,  y - r2,  z, 0, 0,
							x,       y - r,   z, 0, 0,
							x - ir3, y - ir2, z, 0, 0,
							x,       y - ir,  z, 0, 0,
					},
					PointyHexagonDirection.SouthEast => new[] {
							x,       y - r,   z, 0, 0,
							x + r3,  y - r2,  z, 0, 0,
							x,       y - ir,  z, 0, 0,
							x + ir3, y - ir2, z, 0, 0,
					},
					_ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
			};
			//@formatter:on

			return new(vertices, new uint[] { 0, 1, 2, 1, 3, 2, });
		}

		public static Mesh HollowFlatTop(float size, float thickness, float z = 0) => HollowFlatTop(0, 0, size, thickness, z);

		public static Mesh HollowFlatTop(float x, float y, float size, float thickness, float z = 0) {
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }

			float r = size / 2f, r2 = r / 2f, r3 = r * MathH.HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * MathH.HalfSqrt3;
			r3 = -r3;
			ir3 = -ir3;

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

		public static Mesh HollowPointyTop(float size, float thickness, float z = 0) => HollowPointyTop(0, 0, size, thickness, z);

		public static Mesh HollowPointyTop(float x, float y, float size, float thickness, float z = 0) {
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); } else if (thickness <= 0) { throw new ArgumentException("Thickness cannot be equal to or below 0"); }
			float r = size / 2f, r2 = r / 2f, r3 = r * MathH.HalfSqrt3;
			float ir = (size - thickness) / 2f, ir2 = ir / 2f, ir3 = ir * MathH.HalfSqrt3;
			r = -r;
			r2 = -r2;
			ir = -ir;
			ir2 = -ir2;

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

		public static Mesh FlatTop(float size, float z = 0) => FlatTop(0, 0, size, z);

		public static Mesh FlatTop(float x, float y, float size, float z = 0) {
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); }
			float r = size / 2f, r2 = r / 2f, r3 = r * MathH.HalfSqrt3;
			r3 = -r3;

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

		public static Mesh PointyTop(float size, float z = 0) => PointyTop(0, 0, size, z);

		public static Mesh PointyTop(float x, float y, float size, float z = 0) {
			if (size <= 0) { throw new ArgumentException("Size cannot be equal to or below 0"); }
			float r = size / 2f, r2 = r / 2f, r3 = r * MathH.HalfSqrt3;
			r = -r;
			r2 = -r2;

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