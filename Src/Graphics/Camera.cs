using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Graphics {
	[PublicAPI]
	public abstract class Camera {
		public Vector3 Position { get; set; }
		public float NearPlane { get; set; }
		public float FarPlane { get; set; }

		public abstract Matrix4x4 CreateProjectionMatrix();
	}
}