using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Graphics {
	[PublicAPI]
	public class PerspectiveCamera : Camera {
		public float AspectRatio { get; set; }
		public float FovDegrees { get; set; } = 90;
		public float PitchDegrees {
			get;
			set {
				field = value;
				shouldRebuildVectors = true;
			}
		}
		public float YawDegrees {
			get;
			set {
				field = value;
				shouldRebuildVectors = true;
			}
		} = 90;
		// TODO impl roll

		public float FovRadians => float.Pi / 180 * FovDegrees;
		public float PitchRadians => float.Pi / 180 * PitchDegrees;
		public float YawRadians => float.Pi / 180 * YawDegrees;

		public Vector3 Forward { get; private set; }
		public Vector3 Right { get; private set; }
		public Vector3 Backwards => -Forward;
		public Vector3 Left => -Right;

		private bool shouldRebuildVectors = true;

		public PerspectiveCamera(float nearPlane = 0.01f, float farPlane = 100f) {
			NearPlane = nearPlane;
			FarPlane = farPlane;
		}

		public override Matrix4x4 CreateProjectionMatrix() => Matrix4x4.CreatePerspectiveFieldOfView(FovRadians, AspectRatio, NearPlane, FarPlane);

		public Matrix4x4 CreateViewMatrix() {
			if (shouldRebuildVectors) {
				shouldRebuildVectors = false;
				RebuildVectors();
			}

			return Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitY);
		}

		private void RebuildVectors() {
			Forward = Vector3.Normalize(new(MathF.Cos(PitchRadians) * MathF.Cos(YawRadians), MathF.Sin(PitchRadians), MathF.Cos(PitchRadians) * MathF.Sin(YawRadians)));
			Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
		}
	}
}