using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Graphics {
	[PublicAPI]
	public class PerspectiveCamera : Camera {
		public float AspectRatio { get; set; }
		public float FovDegrees { get; set; } = 90;

		public float FovRadians => float.Pi / 180 * FovDegrees;

		public PerspectiveCamera(float aspectRatio, float nearPlane, float farPlane) {
			AspectRatio = aspectRatio;
			NearPlane = nearPlane;
			FarPlane = farPlane;
		}

		public override Matrix4x4 CreateProjectionMatrix() => Matrix4x4.CreatePerspectiveFieldOfView(FovRadians, AspectRatio, NearPlane, FarPlane);
	}
}