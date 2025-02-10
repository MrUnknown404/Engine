using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace USharpLibs.Engine2.Math {
	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	public readonly record struct Pos2d<T> where T : INumber<T> {
		public T X { get; init; }
		public T Y { get; init; }

		public Pos2d(T x, T y) {
			X = x;
			Y = y;
		}

		public void Deconstruct(out T x, out T y) {
			x = X;
			y = Y;
		}

		public static Pos2d<T> operator +(Pos2d<T> value1, Pos2d<T> value2) => new(value1.X + value2.X, value1.Y + value2.Y);
		public static Pos2d<T> operator +(Pos2d<T> value1, T value2) => new(value1.X + value2, value1.Y + value2);
		public static Pos2d<T> operator -(Pos2d<T> value1, Pos2d<T> value2) => new(value1.X - value2.X, value1.Y - value2.Y);
		public static Pos2d<T> operator -(Pos2d<T> value1, T value2) => new(value1.X - value2, value1.Y - value2);
		public static Pos2d<T> operator *(Pos2d<T> value1, Pos2d<T> value2) => new(value1.X * value2.X, value1.Y * value2.Y);
		public static Pos2d<T> operator *(Pos2d<T> value1, T value2) => new(value1.X * value2, value1.Y * value2);
		public static Pos2d<T> operator /(Pos2d<T> value1, Pos2d<T> value2) => new(value1.X / value2.X, value1.Y / value2.Y);
		public static Pos2d<T> operator /(Pos2d<T> value1, T value2) => new(value1.X / value2, value1.Y / value2);

		public Pos2d<T> Add(Direction direction, T amount) =>
				direction switch {
						Direction.North => new(X, Y - amount),
						Direction.East => new(X + amount, Y),
						Direction.South => new(X, Y + amount),
						Direction.West => new(X - amount, Y),
						_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
				};

		public bool Equals(Pos2d<T> other) => X == other.X && Y == other.Y;
		public override int GetHashCode() => HashCode.Combine(X, Y);
		public override string ToString() => $"X: {X}, Y: {Y}";

		public enum Direction : byte {
			North = 0,
			East = 1,
			South = 2,
			West = 3,
		}
	}
}