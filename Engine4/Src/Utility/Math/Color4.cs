using System.Numerics;
using USharpLibs.Common.Math;

namespace Engine4.Utility.Math;

public readonly record struct Color4 {
	public float R { get; init; }
	public float G { get; init; }
	public float B { get; init; }
	public float A { get; init; }

	public Vector4 ToVector => new(R, G, B, A);
	public Vec4<float> ToVec3 => new(R, G, B, A);

	public Color4(Vector4 vector) {
		R = vector.X;
		G = vector.Y;
		B = vector.Z;
		A = vector.W;
	}

	public Color4(float r, float g, float b, float a) {
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public Color4(byte r, byte g, byte b, byte a) {
		R = (float)r / byte.MaxValue;
		G = (float)g / byte.MaxValue;
		B = (float)b / byte.MaxValue;
		A = (float)a / byte.MaxValue;
	}

	public void Deconstruct(out float r, out float g, out float b, out float a) {
		r = R;
		g = G;
		b = B;
		a = A;
	}
}