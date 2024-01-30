using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public readonly struct Pos2d : IEquatable<Pos2d> {
		public int X { get; } = 0;
		public int Y { get; } = 0;

		public Pos2d(int x, int y) {
			X = x;
			Y = y;
		}

		public Pos2d() { }

		public bool Equals(Pos2d other) => X == other.X && Y == other.Y;

		public static bool operator ==(Pos2d tile, Pos2d other) => tile.Equals(other);
		public static bool operator !=(Pos2d tile, Pos2d other) => !tile.Equals(other);

		public static Pos2d operator +(Pos2d tile, Pos2d value) => new(tile.X + value.X, tile.Y + value.Y);
		public static Pos2d operator +(Pos2d tile, int value) => new(tile.X + value, tile.Y + value);

		public static Pos2d operator -(Pos2d tile, Pos2d value) => new(tile.X - value.X, tile.Y - value.Y);
		public static Pos2d operator -(Pos2d tile, int value) => new(tile.X - value, tile.Y - value);

		public static Pos2d operator *(Pos2d tile, Pos2d value) => new(tile.X * value.X, tile.Y * value.Y);
		public static Pos2d operator *(Pos2d tile, int value) => new(tile.X * value, tile.Y * value);

		public static Pos2d operator /(Pos2d tile, Pos2d value) => new(tile.X / value.X, tile.Y / value.Y);
		public static Pos2d operator /(Pos2d tile, int value) => new(tile.X / value, tile.Y / value);

		public override bool Equals(object? obj) => obj is Pos2d other && Equals(other);
		public override int GetHashCode() => (X, Y).GetHashCode();

		public override string ToString() => $"({X}, {Y})";
	}
}