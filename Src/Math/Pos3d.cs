using System.Numerics;
using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public interface IPos3d<out TType, TSelf> : IPos2d<TType, TSelf> where TType : INumber<TType> where TSelf : struct, IPos3d<TType, TSelf> {
		public TType Z { get; }
	}

	[PublicAPI]
	public readonly record struct Pos3d<TType> : IPos3d<TType, Pos3d<TType>> where TType : INumber<TType> {
		public TType X { get; } = default!;
		public TType Y { get; } = default!;
		public TType Z { get; } = default!;

		public Pos3d(TType x, TType y, TType z) {
			X = x;
			Y = y;
			Z = z;
		}

		public Pos3d() { }

		public static Pos3d<TType> operator +(Pos3d<TType> pos, Pos3d<TType> value) => new(pos.X + value.X, pos.Y + value.Y, pos.Z + value.Z);
		public static Pos3d<TType> operator +(Pos3d<TType> pos, TType value) => new(pos.X + value, pos.Y + value, pos.Z + value);

		public static Pos3d<TType> operator -(Pos3d<TType> pos, Pos3d<TType> value) => new(pos.X - value.X, pos.Y - value.Y, pos.Z - value.Z);
		public static Pos3d<TType> operator -(Pos3d<TType> pos, TType value) => new(pos.X - value, pos.Y - value, pos.Z - value);

		public static Pos3d<TType> operator *(Pos3d<TType> pos, Pos3d<TType> value) => new(pos.X * value.X, pos.Y * value.Y, pos.Z * value.Z);
		public static Pos3d<TType> operator *(Pos3d<TType> pos, TType value) => new(pos.X * value, pos.Y * value, pos.Z * value);

		public static Pos3d<TType> operator /(Pos3d<TType> pos, Pos3d<TType> value) => new(pos.X / value.X, pos.Y / value.Y, pos.Z / value.Z);
		public static Pos3d<TType> operator /(Pos3d<TType> pos, TType value) => new(pos.X / value, pos.Y / value, pos.Z / value);

		public override int GetHashCode() => (X, Y, Z).GetHashCode();
		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}