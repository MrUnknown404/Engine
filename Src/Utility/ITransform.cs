using System.Numerics;

namespace Engine3.Utility {
	public interface ITransform<out T> where T : ITransform<T> {
		public static abstract T Zero { get; }

		public Matrix4x4 CreateMatrix();
	}
}