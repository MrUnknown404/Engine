using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Client {
	[PublicAPI]
	public class Camera {
		public Vector3 Position {
			get;
			set {
				field = value;
				isDirectionVectorsDirty = true;
			}
		}

		public Quaternion Orientation { // TODO impl https://www.opengl-tutorial.org/intermediate-tutorials/tutorial-17-quaternions/
			get;
			set {
				field = value;
				isDirectionVectorsDirty = true;
			}
		} = Quaternion.Identity;

		[Obsolete]
		public float PitchDegrees {
			get;
			set {
				field = value;
				if (field is >= 360 or <= -360) { field %= 360; }
				isDirectionVectorsDirty = true;
			}
		}

		[Obsolete]
		public float YawDegrees {
			get;
			set {
				field = value;
				if (field is >= 360 or <= -360) { field %= 360; }
				isDirectionVectorsDirty = true;
			}
		} = 90;

		[Obsolete] public float PitchRadians => float.DegreesToRadians(PitchDegrees);
		[Obsolete] public float YawRadians => float.DegreesToRadians(YawDegrees);

		public Vector3 Forward {
			get {
				if (isDirectionVectorsDirty) {
					RebuildVectors();
					isDirectionVectorsDirty = false;
				}

				return field;
			}
			private set;
		}

		public Vector3 Right {
			get {
				if (isDirectionVectorsDirty) {
					RebuildVectors();
					isDirectionVectorsDirty = false;
				}

				return field;
			}
			private set;
		}

		public Vector3 Backwards => -Forward;
		public Vector3 Left => -Right;

		public bool UseLookAtPosition {
			get;
			set {
				field = value;
				isDirectionVectorsDirty = true;
				isViewDirty = true;
			}
		}

		public Vector3 LookAtPosition {
			get;
			set {
				field = value;
				isDirectionVectorsDirty = true;
				isViewDirty = true;
			}
		}

		public Matrix4x4 Projection {
			get {
				if (isProjectionDirty) {
					field = CameraType switch {
							CameraTypes.Orthographic => Matrix4x4.CreateOrthographic(OrthographicWidth, OrthographicHeight, NearPlane, FarPlane),
							CameraTypes.Perspective => Matrix4x4.CreatePerspectiveFieldOfView(PerspectiveFovRadians, PerspectiveAspectRatio, NearPlane, FarPlane),
							_ => throw new ArgumentOutOfRangeException(),
					};

					isProjectionDirty = false;
				}

				return field;
			}
			private set;
		}

		public Matrix4x4 View {
			get {
				if (isViewDirty) {
					field = Matrix4x4.CreateLookAt(Position, UseLookAtPosition ? LookAtPosition : Position + Forward, Vector3.UnitY);
					isViewDirty = true;
				}

				return field;
			}
			private set;
		}

		public CameraTypes CameraType { get; private set; }

		public float OrthographicWidth {
			get;
			set {
				field = value;
				isProjectionDirty = true;
			}
		}

		public float OrthographicHeight {
			get;
			set {
				field = value;
				isProjectionDirty = true;
			}
		}

		public float PerspectiveAspectRatio {
			get;
			set {
				field = value;
				isProjectionDirty = true;
			}
		}

		public float PerspectiveFovDegrees {
			get;
			set {
				field = value;
				isProjectionDirty = true;
			}
		}

		public float PerspectiveFovRadians => float.DegreesToRadians(PerspectiveFovDegrees);

		public float NearPlane {
			get;
			set {
				field = value;
				isProjectionDirty = true;
			}
		}

		public float FarPlane {
			get;
			set {
				field = value;
				isProjectionDirty = true;
			}
		}

		private bool isProjectionDirty = true;
		private bool isViewDirty = true;
		private bool isDirectionVectorsDirty = true;

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

		public void SetOrthographic(float width, float height) {
			CameraType = CameraTypes.Orthographic;
			OrthographicWidth = width;
			OrthographicHeight = height;
		}

		public void SetPerspective(float aspectRatio, float fov) {
			CameraType = CameraTypes.Perspective;
			PerspectiveAspectRatio = aspectRatio;
			PerspectiveFovDegrees = fov;
		}

		private void RebuildVectors() {
			Vector3 forward = Vector3.Normalize(UseLookAtPosition ? LookAtPosition - Position : new(MathF.Cos(PitchRadians) * MathF.Cos(YawRadians), MathF.Sin(PitchRadians), MathF.Cos(PitchRadians) * MathF.Sin(YawRadians)));

			Forward = forward;
			Right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
		}

		public enum CameraTypes : byte {
			Orthographic,
			Perspective,
		}
	}
}