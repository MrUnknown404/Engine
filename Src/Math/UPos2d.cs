using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public readonly struct UPos2d : IEquatable<UPos2d> {
		public uint X { get; } = 0;
		public uint Y { get; } = 0;

		public UPos2d(uint x, uint y) {
			X = x;
			Y = y;
		}

		public UPos2d() { }

		public bool Equals(UPos2d other) => X == other.X && Y == other.Y;

		public static bool operator ==(UPos2d tile, UPos2d other) => tile.Equals(other);
		public static bool operator !=(UPos2d tile, UPos2d other) => !tile.Equals(other);

		public static UPos2d operator +(UPos2d tile, UPos2d value) => new(tile.X + value.X, tile.Y + value.Y);
		public static UPos2d operator +(UPos2d tile, uint value) => new(tile.X + value, tile.Y + value);

		public static UPos2d operator -(UPos2d tile, UPos2d value) => new(tile.X - value.X, tile.Y - value.Y);
		public static UPos2d operator -(UPos2d tile, uint value) => new(tile.X - value, tile.Y - value);

		public static UPos2d operator *(UPos2d tile, UPos2d value) => new(tile.X * value.X, tile.Y * value.Y);
		public static UPos2d operator *(UPos2d tile, uint value) => new(tile.X * value, tile.Y * value);

		public static UPos2d operator /(UPos2d tile, UPos2d value) => new(tile.X / value.X, tile.Y / value.Y);
		public static UPos2d operator /(UPos2d tile, uint value) => new(tile.X / value, tile.Y / value);

		public override bool Equals(object? obj) => obj is UPos2d other && Equals(other);
		public override int GetHashCode() => (X, Y).GetHashCode();

		public override string ToString() => $"({X}, {Y})";
	}
}