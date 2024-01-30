using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public readonly struct Pos3df : IEquatable<Pos3df> {
		public float X { get; } = 0;
		public float Y { get; } = 0;
		public float Z { get; } = 0;

		public Pos3df(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		public Pos3df() { }

		// ReSharper disable CompareOfFloatsByEqualityOperator
		public bool Equals(Pos3df other) => X == other.X && Y == other.Y && Z == other.Z;

		public static bool operator ==(Pos3df cube, Pos3df other) => cube.Equals(other);
		public static bool operator !=(Pos3df cube, Pos3df other) => !cube.Equals(other);

		public static Pos3df operator +(Pos3df cube, Pos3df value) => new(cube.X + value.X, cube.Y + value.Y, cube.Z + value.Z);
		public static Pos3df operator +(Pos3df cube, float value) => new(cube.X + value, cube.Y + value, cube.Z + value);

		public static Pos3df operator -(Pos3df cube, Pos3df value) => new(cube.X - value.X, cube.Y - value.Y, cube.Z - value.Z);
		public static Pos3df operator -(Pos3df cube, float value) => new(cube.X - value, cube.Y - value, cube.Z - value);

		public static Pos3df operator *(Pos3df cube, Pos3df value) => new(cube.X * value.X, cube.Y * value.Y, cube.Z * value.Z);
		public static Pos3df operator *(Pos3df cube, float value) => new(cube.X * value, cube.Y * value, cube.Z * value);

		public static Pos3df operator /(Pos3df cube, Pos3df value) => new(cube.X / value.X, cube.Y / value.Y, cube.Z / value.Z);
		public static Pos3df operator /(Pos3df cube, float value) => new(cube.X / value, cube.Y / value, cube.Z / value);

		public override bool Equals(object? obj) => obj is Pos3df other && Equals(other);
		public override int GetHashCode() => (X, Y, Z).GetHashCode();

		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}