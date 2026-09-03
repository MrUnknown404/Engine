using System.Numerics;

namespace Engine4.Utility.Math;

public readonly record struct Color3 {
	public float R { get; init; }
	public float G { get; init; }
	public float B { get; init; }

	public Vector3 ToVector => new(R, G, B);

	public Color3(Vector3 vector) {
		R = vector.X;
		G = vector.Y;
		B = vector.Z;
	}

	public Color3(float r, float g, float b) {
		R = r;
		G = g;
		B = b;
	}
}