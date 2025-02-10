using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace USharpLibs.Engine2.Math {
	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	public readonly record struct Pos3d<T> where T : INumber<T> {
		public T X { get; init; }
		public T Y { get; init; }
		public T Z { get; init; }

		public Pos3d(T x, T y, T z) {
			X = x;
			Y = y;
			Z = z;
		}

		public void Deconstruct(out T x, out T y, out T z) {
			x = X;
			y = Y;
			z = Z;
		}

		public static Pos3d<T> operator +(Pos3d<T> value1, Pos3d<T> value2) => new(value1.X + value2.X, value1.Y + value2.Y, value1.Z + value2.Z);
		public static Pos3d<T> operator +(Pos3d<T> value1, T value2) => new(value1.X + value2, value1.Y + value2, value1.Z + value2);
		public static Pos3d<T> operator -(Pos3d<T> value1, Pos3d<T> value2) => new(value1.X - value2.X, value1.Y - value2.Y, value1.Z - value2.Z);
		public static Pos3d<T> operator -(Pos3d<T> value1, T value2) => new(value1.X - value2, value1.Y - value2, value1.Z - value2);
		public static Pos3d<T> operator *(Pos3d<T> value1, Pos3d<T> value2) => new(value1.X * value2.X, value1.Y * value2.Y, value1.Z * value2.Z);
		public static Pos3d<T> operator *(Pos3d<T> value1, T value2) => new(value1.X * value2, value1.Y * value2, value1.Z * value2);
		public static Pos3d<T> operator /(Pos3d<T> value1, Pos3d<T> value2) => new(value1.X / value2.X, value1.Y / value2.Y, value1.Z / value2.Z);
		public static Pos3d<T> operator /(Pos3d<T> value1, T value2) => new(value1.X / value2, value1.Y / value2, value1.Z / value2);

		public Pos3d<T> Add(Direction direction, T amount) =>
				direction switch {
						Direction.Up => new(X, Y - amount, Z),
						Direction.Down => new(X, Y + amount, Z),
						Direction.North => new(X, Y, Z - amount),
						Direction.East => new(X + amount, Y, Z),
						Direction.South => new(X, Y, Z + amount),
						Direction.West => new(X - amount, Y, Z),
						_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
				};

		public bool Equals(Pos3d<T> other) => X == other.X && Y == other.Y && Z == other.Z;
		public override int GetHashCode() => HashCode.Combine(X, Y, Z);
		public override string ToString() => $"X: {X}, Y: {Y}, Z: {Z}";

		public enum Direction : byte {
			Up = 0,
			Down = 1,
			North = 2,
			East = 3,
			South = 4,
			West = 5,
		}
	}
}