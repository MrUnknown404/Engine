using System.Numerics;
using Engine3.Utility;
using JetBrains.Annotations;

namespace Engine3.Client {
	public class CameraTransform : ITransform<CameraTransform> { // TODO impl rotation
		public static CameraTransform Zero => new();

		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; } // TODO use quaternions https://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/

		public Matrix4x4 CreateMatrix() => throw new NotImplementedException(); // TODO create matrix
	}

	[PublicAPI]
	public class Camera {
		public CameraTransform Transform { get; } = new();

		[Obsolete]
		public float PitchDegrees {
			get;
			set {
				field = value;
				if (field is >= 360 or <= -360) { field %= 360; }
				shouldRebuildVectors = true;
			}
		}

		[Obsolete]
		public float YawDegrees {
			get;
			set {
				field = value;
				if (field is >= 360 or <= -360) { field %= 360; }
				shouldRebuildVectors = true;
			}
		} = 90;

		public float PitchRadians => float.DegreesToRadians(PitchDegrees);
		public float YawRadians => float.DegreesToRadians(YawDegrees);

		public Vector3 Forward { get; private set; }
		public Vector3 Right { get; private set; }
		public Vector3 Backwards => -Forward;
		public Vector3 Left => -Right;

		public bool UseLookAtPosition {
			get;
			set {
				field = value;
				shouldRebuildVectors = true;
			}
		}

		public Vector3 LookAtPosition {
			get;
			set {
				field = value;
				shouldRebuildVectors = true;
			}
		}

		public CameraTypes CameraType { get; private set; }

		public float OrthographicWidth { get; set; }
		public float OrthographicHeight { get; set; }

		public float PerspectiveAspectRatio { get; set; }
		public float PerspectiveFovDegrees { get; set; }
		public float PerspectiveFovRadians => float.DegreesToRadians(PerspectiveFovDegrees);

		public float NearPlane { get; set; }
		public float FarPlane { get; set; }

		private bool shouldRebuildVectors = true;

		private Camera(float nearPlane, float farPlane) {
			NearPlane = nearPlane;
			FarPlane = farPlane;
		}

		[MustUseReturnValue]
		public static Camera CreateOrthographic(float width, float height, float nearPlane, float farPlane) {
			Camera camera = new(nearPlane, farPlane);
			camera.SetOrthographic(width, height);
			return camera;
		}

		[MustUseReturnValue]
		public static Camera CreatePerspective(float aspectRatio, float fov, float nearPlane, float farPlane) {
			Camera camera = new(nearPlane, farPlane);
			camera.SetPerspective(aspectRatio, fov);
			return camera;
		}

		private void SetOrthographic(float width, float height) {
			CameraType = CameraTypes.Orthographic;
			OrthographicWidth = width;
			OrthographicHeight = height;
		}

		private void SetPerspective(float aspectRatio, float fov) {
			CameraType = CameraTypes.Perspective;
			PerspectiveAspectRatio = aspectRatio;
			PerspectiveFovDegrees = fov;
		}

		[MustUseReturnValue]
		public Matrix4x4 CreateProjectionMatrix() =>
				CameraType switch {
						CameraTypes.Orthographic => Matrix4x4.CreateOrthographic(OrthographicWidth, OrthographicHeight, NearPlane, FarPlane),
						CameraTypes.Perspective => Matrix4x4.CreatePerspectiveFieldOfView(PerspectiveFovRadians, PerspectiveAspectRatio, NearPlane, FarPlane),
						_ => throw new ArgumentOutOfRangeException(),
				};

		[MustUseReturnValue]
		public Matrix4x4 CreateViewMatrix() {
			if (shouldRebuildVectors) {
				RebuildVectors();
				shouldRebuildVectors = false;
			}

			return Matrix4x4.CreateLookAt(Transform.Position, UseLookAtPosition ? LookAtPosition : Transform.Position + Forward, Vector3.UnitY);
		}

		private void RebuildVectors() {
			Forward = Vector3.Normalize(UseLookAtPosition ? LookAtPosition - Transform.Position : new(MathF.Cos(PitchRadians) * MathF.Cos(YawRadians), MathF.Sin(PitchRadians), MathF.Cos(PitchRadians) * MathF.Sin(YawRadians)));
			Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
		}

		public enum CameraTypes : byte {
			Orthographic,
			Perspective,
		}
	}
}