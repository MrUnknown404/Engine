using System.Numerics;
using JetBrains.Annotations;

namespace Engine3.Utility {
	[PublicAPI]
	public class Transform {
		public Vector3 Position { get; set; }
		public Vector3 Scale { get; set; }
		public Quaternion Rotation { get; set; }

		public Matrix4x4 GetMatrix() { // untested. i think this is right?
			Matrix4x4 matrix = Matrix4x4.Identity;
			matrix *= Matrix4x4.CreateTranslation(Position);
			matrix *= Matrix4x4.Transform(matrix, Rotation);
			matrix *= Matrix4x4.CreateScale(Scale);
			return matrix;
		}
	}
}