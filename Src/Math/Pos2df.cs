using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public readonly struct Pos2df : IEquatable<Pos2df> {
		public float X { get; } = 0;
		public float Y { get; } = 0;

		public Pos2df(float x, float y) {
			X = x;
			Y = y;
		}

		public Pos2df() { }

		// ReSharper disable CompareOfFloatsByEqualityOperator
		public bool Equals(Pos2df other) => X == other.X && Y == other.Y;

		public static bool operator ==(Pos2df tile, Pos2df other) => tile.Equals(other);
		public static bool operator !=(Pos2df tile, Pos2df other) => !tile.Equals(other);

		public static Pos2df operator +(Pos2df tile, Pos2df value) => new(tile.X + value.X, tile.Y + value.Y);
		public static Pos2df operator +(Pos2df tile, float value) => new(tile.X + value, tile.Y + value);

		public static Pos2df operator -(Pos2df tile, Pos2df value) => new(tile.X - value.X, tile.Y - value.Y);
		public static Pos2df operator -(Pos2df tile, float value) => new(tile.X - value, tile.Y - value);

		public static Pos2df operator *(Pos2df tile, Pos2df value) => new(tile.X * value.X, tile.Y * value.Y);
		public static Pos2df operator *(Pos2df tile, float value) => new(tile.X * value, tile.Y * value);

		public static Pos2df operator /(Pos2df tile, Pos2df value) => new(tile.X / value.X, tile.Y / value.Y);
		public static Pos2df operator /(Pos2df tile, float value) => new(tile.X / value, tile.Y / value);

		public override bool Equals(object? obj) => obj is Pos2df other && Equals(other);
		public override int GetHashCode() => (X, Y).GetHashCode();

		public override string ToString() => $"({X}, {Y})";
	}
}