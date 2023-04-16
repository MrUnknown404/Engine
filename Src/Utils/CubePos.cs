using JetBrains.Annotations;

namespace USharpLibs.Engine.Utils {
	[PublicAPI]
	public readonly struct CubePos : IEquatable<CubePos> {
		public int X { get; } = 0;
		public int Y { get; } = 0;
		public int Z { get; } = 0;

		public CubePos(int x, int y, int z) {
			X = x;
			Y = y;
			Z = z;
		}

		public CubePos() { }

		public bool Equals(CubePos other) => X == other.X && Y == other.Y && Z == other.Z;

		public static bool operator ==(CubePos cube, CubePos other) => cube.Equals(other);
		public static bool operator !=(CubePos cube, CubePos other) => !cube.Equals(other);

		public static CubePos operator +(CubePos cube, CubePos value) => new(cube.X + value.X, cube.Y + value.Y, cube.Z + value.Z);
		public static CubePos operator +(CubePos cube, int value) => new(cube.X + value, cube.Y + value, cube.Z + value);

		public static CubePos operator -(CubePos cube, CubePos value) => new(cube.X - value.X, cube.Y - value.Y, cube.Z - value.Z);
		public static CubePos operator -(CubePos cube, int value) => new(cube.X - value, cube.Y - value, cube.Z - value);

		public static CubePos operator *(CubePos cube, CubePos value) => new(cube.X * value.X, cube.Y * value.Y, cube.Z * value.Z);
		public static CubePos operator *(CubePos cube, int value) => new(cube.X * value, cube.Y * value, cube.Z * value);

		public static CubePos operator /(CubePos cube, CubePos value) => new(cube.X / value.X, cube.Y / value.Y, cube.Z / value.Z);
		public static CubePos operator /(CubePos cube, int value) => new(cube.X / value, cube.Y / value, cube.Z / value);

		public override bool Equals(object? obj) => obj is CubePos other && Equals(other);
		public override int GetHashCode() => (X, Y, Z).GetHashCode();

		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}