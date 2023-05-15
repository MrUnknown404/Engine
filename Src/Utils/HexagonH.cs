using System.Numerics;
using JetBrains.Annotations;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Utils {
	[PublicAPI]
	public static class HexagonH {
		public static void FlatTopHexPosToXY(in HexPos pos, out float x, out float y, uint radius) {
			x = radius * (1.5f * pos.Q);
			y = radius * (MathH.HalfSqrt3 * pos.Q + MathH.Sqrt3 * pos.R);
		}

		public static void PointyTopHexPosToXY(in HexPos pos, out float x, out float y, uint radius) {
			x = radius * (MathH.Sqrt3 * pos.Q + MathH.HalfSqrt3 * pos.R);
			y = radius * (1.5f * pos.R);
		}

		[MustUseReturnValue]
		public static Vector2 FlatTopHexPosToXY(in HexPos pos, uint radius) {
			FlatTopHexPosToXY(pos, out float x, out float y, radius);
			return new(x, y);
		}

		[MustUseReturnValue]
		public static Vector2 PointyTopHexPosToXY(in HexPos pos, uint radius) {
			PointyTopHexPosToXY(pos, out float x, out float y, radius);
			return new(x, y);
		}

		[MustUseReturnValue]
		public static HexPos XYToFlatTopHexPos(int x, int y, uint radius) {
			const float A = 1f / 3f, B = -2f / 3f, C = 1f / MathH.Sqrt3;

			float cx = (float)-x / radius, cy = (float)y / radius;

			float fx = B * cx;
			float fy = A * cx + C * cy;
			float fz = A * cx - C * cy;

			int a = MathH.Ceil(fx - fy);
			int b = MathH.Ceil(fy - fz);
			int c = MathH.Ceil(fz - fx);

			return new(MathH.Round((a - c) / 3f), MathH.Round((b - a) / 3f), MathH.Round((c - b) / 3f));
		}

		// TODO XYToPointyTopHexPos
		[MustUseReturnValue] public static HexPos XYToPointyTopHexPos(int x, int y, uint radius) => throw new NotImplementedException();
	}
}