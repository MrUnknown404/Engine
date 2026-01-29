using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.GameObject {
	[PublicAPI]
	public class Transform2D : ITransform<Transform2D, Vector3, Vector2, uint> {
		public static Transform2D Zero => new();

		public Vector3 Position { get; set; }
		public Vector2 Scale { get; set; }
		public uint Rotation { get; set; }

		public Matrix4x4 CreateMatrix() {
			Matrix4x4 matrix = Matrix4x4.Identity;
			matrix *= Matrix4x4.CreateTranslation(Position);
			matrix *= Matrix4x4.CreateRotationX(Rotation);
			matrix *= Matrix4x4.CreateScale(Scale.X, Scale.Y, 0);
			return matrix;
		}

		public bool Equals(Transform2D? other) => other != null && Position.Equals(other.Position) && Scale.Equals(other.Scale) && Rotation == other.Rotation;
		public override bool Equals(object? obj) => obj is Transform2D transform && Equals(transform);

		public override int GetHashCode() => HashCode.Combine(Position, Scale, Rotation);

		public static bool operator ==(Transform2D? left, Transform2D? right) => Equals(left, right);
		public static bool operator !=(Transform2D? left, Transform2D? right) => !Equals(left, right);
	}
}