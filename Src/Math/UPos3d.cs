using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public readonly struct UPos3d : IEquatable<UPos3d> {
		public uint X { get; } = 0;
		public uint Y { get; } = 0;
		public uint Z { get; } = 0;

		public UPos3d(uint x, uint y, uint z) {
			X = x;
			Y = y;
			Z = z;
		}

		public UPos3d() { }

		public bool Equals(UPos3d other) => X == other.X && Y == other.Y && Z == other.Z;

		public static bool operator ==(UPos3d cube, UPos3d other) => cube.Equals(other);
		public static bool operator !=(UPos3d cube, UPos3d other) => !cube.Equals(other);

		public static UPos3d operator +(UPos3d cube, UPos3d value) => new(cube.X + value.X, cube.Y + value.Y, cube.Z + value.Z);
		public static UPos3d operator +(UPos3d cube, uint value) => new(cube.X + value, cube.Y + value, cube.Z + value);

		public static UPos3d operator -(UPos3d cube, UPos3d value) => new(cube.X - value.X, cube.Y - value.Y, cube.Z - value.Z);
		public static UPos3d operator -(UPos3d cube, uint value) => new(cube.X - value, cube.Y - value, cube.Z - value);

		public static UPos3d operator *(UPos3d cube, UPos3d value) => new(cube.X * value.X, cube.Y * value.Y, cube.Z * value.Z);
		public static UPos3d operator *(UPos3d cube, uint value) => new(cube.X * value, cube.Y * value, cube.Z * value);

		public static UPos3d operator /(UPos3d cube, UPos3d value) => new(cube.X / value.X, cube.Y / value.Y, cube.Z / value.Z);
		public static UPos3d operator /(UPos3d cube, uint value) => new(cube.X / value, cube.Y / value, cube.Z / value);

		public override bool Equals(object? obj) => obj is UPos3d other && Equals(other);
		public override int GetHashCode() => (X, Y, Z).GetHashCode();

		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}