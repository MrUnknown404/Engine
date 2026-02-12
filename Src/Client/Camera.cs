using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Client {
	[PublicAPI]
	public abstract class Camera {
		public Vector3 Position { get; set; }

		public float NearPlane { get; set; }
		public float FarPlane { get; set; }

		public float PitchDegrees { // TODO use quaternions https://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/
			get;
			set {
				field = value;
				if (field is >= 360 or <= -360) { field %= 360; }
				shouldRebuildVectors = true;
			}
		}

		public float YawDegrees {
			get;
			set {
				field = value;
				if (field is >= 360 or <= -360) { field %= 360; }
				shouldRebuildVectors = true;
			}
		} = 90;
		// TODO impl roll

		public float PitchRadians => float.Pi / 180 * PitchDegrees;
		public float YawRadians => float.Pi / 180 * YawDegrees;

		public bool UseLookAtPosition { get; set; }
		public Vector3 LookAtPosition { get; set; }

		public Vector3 Forward { get; private set; }
		public Vector3 Right { get; private set; }
		public Vector3 Backwards => -Forward;
		public Vector3 Left => -Right;

		private bool shouldRebuildVectors = true;

		public abstract Matrix4x4 CreateProjectionMatrix();

		public Matrix4x4 CreateViewMatrix() {
			if (shouldRebuildVectors && !UseLookAtPosition) {
				RebuildVectors();
				shouldRebuildVectors = false;
			}

			return Matrix4x4.CreateLookAt(Position, UseLookAtPosition ? LookAtPosition : Position + Forward, Vector3.UnitY);
		}

		private void RebuildVectors() {
			Forward = Vector3.Normalize(new(MathF.Cos(PitchRadians) * MathF.Cos(YawRadians), MathF.Sin(PitchRadians), MathF.Cos(PitchRadians) * MathF.Sin(YawRadians)));
			Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
		}
	}
}