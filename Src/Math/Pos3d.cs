using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public readonly struct Pos3d : IEquatable<Pos3d> {
		public int X { get; } = 0;
		public int Y { get; } = 0;
		public int Z { get; } = 0;

		public Pos3d(int x, int y, int z) {
			X = x;
			Y = y;
			Z = z;
		}

		public Pos3d() { }

		public bool Equals(Pos3d other) => X == other.X && Y == other.Y && Z == other.Z;

		public static bool operator ==(Pos3d cube, Pos3d other) => cube.Equals(other);
		public static bool operator !=(Pos3d cube, Pos3d other) => !cube.Equals(other);

		public static Pos3d operator +(Pos3d cube, Pos3d value) => new(cube.X + value.X, cube.Y + value.Y, cube.Z + value.Z);
		public static Pos3d operator +(Pos3d cube, int value) => new(cube.X + value, cube.Y + value, cube.Z + value);

		public static Pos3d operator -(Pos3d cube, Pos3d value) => new(cube.X - value.X, cube.Y - value.Y, cube.Z - value.Z);
		public static Pos3d operator -(Pos3d cube, int value) => new(cube.X - value, cube.Y - value, cube.Z - value);

		public static Pos3d operator *(Pos3d cube, Pos3d value) => new(cube.X * value.X, cube.Y * value.Y, cube.Z * value.Z);
		public static Pos3d operator *(Pos3d cube, int value) => new(cube.X * value, cube.Y * value, cube.Z * value);

		public static Pos3d operator /(Pos3d cube, Pos3d value) => new(cube.X / value.X, cube.Y / value.Y, cube.Z / value.Z);
		public static Pos3d operator /(Pos3d cube, int value) => new(cube.X / value, cube.Y / value, cube.Z / value);

		public override bool Equals(object? obj) => obj is Pos3d other && Equals(other);
		public override int GetHashCode() => (X, Y, Z).GetHashCode();

		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}