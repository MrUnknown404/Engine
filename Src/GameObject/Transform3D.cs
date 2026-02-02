using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.GameObject {
	[PublicAPI]
	public class Transform3D : ITransform<Transform3D>, ITransformPosition<Vector3>, ITransformScale<Vector3>, ITransformRotation<Quaternion> {
		public static Transform3D Zero => new();

		public Vector3 Position { get; set; }
		public Vector3 Scale { get; set; }
		public Quaternion Rotation { get; set; }

		public Matrix4x4 CreateMatrix() {
			Matrix4x4 matrix = Matrix4x4.Identity;
			matrix *= Matrix4x4.CreateTranslation(Position);
			matrix *= Matrix4x4.Transform(matrix, Rotation);
			matrix *= Matrix4x4.CreateScale(Scale);
			return matrix;
		}

		public bool Equals(Transform3D? other) => other != null && Position.Equals(other.Position) && Scale.Equals(other.Scale) && Rotation == other.Rotation;
		public override bool Equals(object? obj) => obj is Transform3D transform && Equals(transform);

		public override int GetHashCode() => HashCode.Combine(Position, Scale, Rotation);

		public static bool operator ==(Transform3D? left, Transform3D? right) => Equals(left, right);
		public static bool operator !=(Transform3D? left, Transform3D? right) => !Equals(left, right);
	}
}