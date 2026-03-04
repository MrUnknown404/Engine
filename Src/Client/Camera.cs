using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Client {
	[PublicAPI]
	public class Camera {
		public Vector3 Position {
			get;
			set {
				field = value;
				isViewDirty = true;
			}
		}

		public Quaternion Orientation {
			get;
			set {
				field = value;
				isDirectionVectorsDirty = true;
				isViewDirty = true;
			}
		} = Quaternion.Identity;

		public Vector3 Forward {
			get {
				if (isDirectionVectorsDirty) { RebuildVectors(); }
				return field;
			}
			private set;
		} = Vector3.UnitZ;

		public Vector3 Right {
			get {
				if (isDirectionVectorsDirty) { RebuildVectors(); }
				return field;
			}
			private set;
		} = Vector3.UnitX;

		public Vector3 Up {
			get {
				if (isDirectionVectorsDirty) { RebuildVectors(); }
				return field;
			}
			private set;
		} = Vector3.UnitY;

		public Vector3 Backwards => -Forward;
		public Vector3 Left => -Right;
		public Vector3 Down => -Up;

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

					if (FlipY) { field = field with { M22 = -field.M22, }; }

					isProjectionDirty = false;
				}

				return field;
			}
			private set;
		}

		public Matrix4x4 View {
			get {
				if (isViewDirty) {
					field = UseLookAtPosition ? Matrix4x4.CreateLookAt(Position, LookAtPosition, Up) : Matrix4x4.Transform(Matrix4x4.CreateTranslation(-Position), Orientation);
					isViewDirty = false;
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

		public bool FlipY { get; set; } = true;

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

		private void RebuildVectors() { // TODO is my math wrong? why do i need to flip Y all over the place?
			Vector3 forward = Vector3.Normalize(MultiplyQuaternion(Orientation, -Vector3.UnitZ)); // flipping z seems to be the cause of the weirdness
			Vector3 up = Vector3.Normalize(MultiplyQuaternion(Orientation, -Vector3.UnitY));
			Vector3 right = Vector3.Cross(up, forward);

			Forward = forward with { Y = -forward.Y, };
			Right = right;
			Up = up with { Y = -up.Y, };

			isDirectionVectorsDirty = false;
			isViewDirty = true;

			return;

			Vector3 MultiplyQuaternion(Quaternion q, Vector3 v) { // taken from stackoverflow (glm?) // TODO make extension?
				Vector3 quatVector = new(q.X, -q.Y, q.Z);
				Vector3 uv = Vector3.Cross(quatVector, v);
				Vector3 uuv = Vector3.Cross(quatVector, uv);
				return v + (uv * q.W + uuv) * 2;
			}

			// GLM_FUNC_QUALIFIER GLM_CONSTEXPR vec < 3, T, Q > operator*(qua < T, Q > const&q, vec < 3, T, Q > const&v)
			// {
			// 	vec < 3, T, Q > const QuatVector (q.x, q.y, q.z);
			// 	vec < 3, T, Q > const uv (glm::cross(QuatVector, v));
			// 	vec < 3, T, Q > const uuv (glm::cross(QuatVector, uv));
			//
			// 	return v + ((uv * q.w) + uuv) * static_cast<T>(2);
			// }
		}

		public enum CameraTypes : byte {
			Orthographic,
			Perspective,
		}
	}
}