using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Client {
	[PublicAPI]
	public class OrthographicCamera : Camera {
		public float Width { get; set; }
		public float Height { get; set; }

		public OrthographicCamera(float width, float height, float nearPlane, float farPlane) {
			Width = width;
			Height = height;
			NearPlane = nearPlane;
			FarPlane = farPlane;
		}

		public override Matrix4x4 CreateProjectionMatrix() => Matrix4x4.CreateOrthographic(Width, Height, NearPlane, FarPlane);
	}
}