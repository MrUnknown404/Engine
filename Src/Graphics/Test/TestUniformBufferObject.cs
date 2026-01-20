using System.Numerics;

namespace Engine3.Graphics.Test {
	public class TestUniformBufferObject {
		private static unsafe uint MatrixSize { get; } = (uint)(sizeof(Matrix4x4));
		public static unsafe uint Size { get; } = (uint)(sizeof(Matrix4x4) * 3);

		public Matrix4x4 Projection { get; set; } = Matrix4x4.Identity;
		public Matrix4x4 View { get; set; } = Matrix4x4.Identity;
		public Matrix4x4 Model { get; set; } = Matrix4x4.Identity;

		public unsafe byte[] CollectBytes() {
			byte[] bytes = new byte[Size];

			CollectBytes(ref bytes, 0, Projection);
			CollectBytes(ref bytes, MatrixSize, View);
			CollectBytes(ref bytes, MatrixSize * 2, Model);

			return bytes;

			static void CollectBytes<T>(ref byte[] bytes, uint startingIndex, T value) where T : unmanaged {
				byte* pointer = (byte*)&value;
				for (int i = 0; i < sizeof(T); i++) { bytes[startingIndex + i] = pointer[i]; }
			}
		}
	}
}