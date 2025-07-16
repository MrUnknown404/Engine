using System.Numerics;
using JetBrains.Annotations;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public interface IPos2d<out TType, TSelf> where TType : INumber<TType> where TSelf : struct, IPos2d<TType, TSelf> {
		public TType X { get; }
		public TType Y { get; }
	}

	[PublicAPI]
	public readonly record struct Pos2d<TType> : IPos2d<TType, Pos2d<TType>> where TType : INumber<TType> {
		public TType X { get; } = default!;
		public TType Y { get; } = default!;

		public Pos2d(TType x, TType y) {
			X = x;
			Y = y;
		}

		public Pos2d() { }

		public static Pos2d<TType> operator +(Pos2d<TType> pos, Pos2d<TType> value) => new(pos.X + value.X, pos.Y + value.Y);
		public static Pos2d<TType> operator +(Pos2d<TType> pos, TType value) => new(pos.X + value, pos.Y + value);

		public static Pos2d<TType> operator -(Pos2d<TType> pos, Pos2d<TType> value) => new(pos.X - value.X, pos.Y - value.Y);
		public static Pos2d<TType> operator -(Pos2d<TType> pos, TType value) => new(pos.X - value, pos.Y - value);

		public static Pos2d<TType> operator *(Pos2d<TType> pos, Pos2d<TType> value) => new(pos.X * value.X, pos.Y * value.Y);
		public static Pos2d<TType> operator *(Pos2d<TType> pos, TType value) => new(pos.X * value, pos.Y * value);

		public static Pos2d<TType> operator /(Pos2d<TType> pos, Pos2d<TType> value) => new(pos.X / value.X, pos.Y / value.Y);
		public static Pos2d<TType> operator /(Pos2d<TType> pos, TType value) => new(pos.X / value, pos.Y / value);

		public override int GetHashCode() => (X, Y).GetHashCode();
		public override string ToString() => $"({X}, {Y})";
	}
}