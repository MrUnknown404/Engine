using JetBrains.Annotations;

namespace USharpLibs.Engine.Utils {
	[PublicAPI]
	public readonly struct TilePos : IEquatable<TilePos> {
		public int X { get; } = 0;
		public int Y { get; } = 0;

		public TilePos(int x, int y) {
			X = x;
			Y = y;
		}

		public TilePos() { }

		public bool Equals(TilePos other) => X == other.X && Y == other.Y;

		public static bool operator ==(TilePos tile, TilePos other) => tile.Equals(other);
		public static bool operator !=(TilePos tile, TilePos other) => !tile.Equals(other);

		public static TilePos operator +(TilePos tile, TilePos value) => new(tile.X + value.X, tile.Y + value.Y);
		public static TilePos operator +(TilePos tile, int value) => new(tile.X + value, tile.Y + value);

		public static TilePos operator -(TilePos tile, TilePos value) => new(tile.X - value.X, tile.Y - value.Y);
		public static TilePos operator -(TilePos tile, int value) => new(tile.X - value, tile.Y - value);

		public static TilePos operator *(TilePos tile, TilePos value) => new(tile.X * value.X, tile.Y * value.Y);
		public static TilePos operator *(TilePos tile, int value) => new(tile.X * value, tile.Y * value);

		public static TilePos operator /(TilePos tile, TilePos value) => new(tile.X / value.X, tile.Y / value.Y);
		public static TilePos operator /(TilePos tile, int value) => new(tile.X / value, tile.Y / value);

		public override bool Equals(object? obj) => obj is TilePos other && Equals(other);
		public override int GetHashCode() => (X, Y).GetHashCode();

		public override string ToString() => $"({X}, {Y})";
	}
}