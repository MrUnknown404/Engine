using System.Numerics;

namespace Engine3.GameObject {
	public interface ITransform<TSelf> : IEquatable<TSelf> where TSelf : ITransform<TSelf> {
		public static abstract TSelf Zero { get; }

		public Matrix4x4 CreateMatrix();
	}

	public interface ITransformPosition<T> where T : unmanaged {
		public T Position { get; set; }
	}

	public interface ITransformRotation<T> where T : unmanaged {
		public T Rotation { get; set; }
	}

	public interface ITransformScale<T> where T : unmanaged {
		public T Scale { get; set; }
	}
}