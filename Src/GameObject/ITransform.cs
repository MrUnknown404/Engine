using System.Numerics;

namespace Engine3.GameObject {
	public interface ITransform<TSelf> : IEquatable<TSelf> where TSelf : ITransform<TSelf> {
		public static abstract TSelf Zero { get; }

		public Matrix4x4 CreateMatrix();
	}

	public interface ITransform<TSelf, TPos> : ITransform<TSelf> where TSelf : ITransform<TSelf> where TPos : unmanaged {
		public TPos Position { get; set; }
	}

	public interface ITransform<TSelf, TPos, TScale> : ITransform<TSelf, TPos> where TSelf : ITransform<TSelf> where TPos : unmanaged where TScale : unmanaged {
		public TScale Scale { get; set; }
	}

	public interface ITransform<TSelf, TPos, TScale, TRot> : ITransform<TSelf, TPos, TScale> where TSelf : ITransform<TSelf> where TPos : unmanaged where TScale : unmanaged where TRot : unmanaged {
		public TRot Rotation { get; set; }
	}
}