using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using USharpLibs.Common.IO;

namespace USharpLibs.Engine2.Math {
	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	public readonly record struct HexPos<T> where T : IBinaryInteger<T>, ISignedNumber<T> {
		public static HexPos<T> Identity { get; }

		public T Q { get; } = T.Zero;
		public T R { get; } = T.Zero;
		public T S => -Q - R;

		public HexPos(T q, T r) {
			if (q + r + S != T.Zero) {
				Logger.Warn($"Attempted to create HexPos with invalid coordinates: ({q}, {r}, {-q - r}). Hexagon coordinates when added together must equal 0.");
				return;
			}

			Q = q;
			R = r;
		}

		public void Deconstruct(out T q, out T r) {
			q = Q;
			r = R;
		}

		public void Deconstruct(out T q, out T r, out T s) {
			q = Q;
			r = R;
			s = S;
		}

		// TODO operators

		public HexPos<T> Add(Direction direction, T amount) =>
				direction switch {
						Direction.SIncrementRDecrement => new(Q, R - amount),
						Direction.RDecrementQIncrement => new(Q + amount, R - amount),
						Direction.QIncrementSDecrement => new(Q + amount, R),
						Direction.SDecrementRIncrement => new(Q, R + amount),
						Direction.RIncrementQDecrement => new(Q - amount, R + amount),
						Direction.QDecrementSIncrement => new(Q - amount, R),
						_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
				};

		public bool Equals(HexPos<T> other) => Q == other.Q && R == other.R && S == other.S;
		public override int GetHashCode() => HashCode.Combine(Q, R, S);
		public override string ToString() => $"Q: {Q}, R: {R}, S: {S}";

		public enum Direction : byte {
			SIncrementRDecrement = 0,
			RDecrementQIncrement = 1,
			QIncrementSDecrement = 2,
			SDecrementRIncrement = 3,
			RIncrementQDecrement = 4,
			QDecrementSIncrement = 5,
		}
	}
}