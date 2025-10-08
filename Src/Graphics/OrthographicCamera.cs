using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Graphics {
	[PublicAPI]
	public class OrthographicCamera : Camera {
		public float MinX { get; set; } = -1;
		public float MinY { get; set; } = -1;
		public float MaxX { get; set; } = 1;
		public float MaxY { get; set; } = 1;

		public OrthographicCamera(float nearPlane = -1, float farPlane = 1) {
			NearPlane = nearPlane;
			FarPlane = farPlane;
		}

		public override Matrix4x4 CreateProjectionMatrix() => Matrix4x4.CreateOrthographicOffCenter(MinX, MaxX, MaxY, MinY, NearPlane, FarPlane);
	}
}