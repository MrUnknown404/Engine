using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models.Separated {
	[StructLayout(LayoutKind.Explicit)]
	public readonly record struct VertexAttribUv : IVertexAttribute {
		public static VertexLayout VertexLayout { get; } = new(VertexAttribPointerType.Float, 2);
		public static byte SizeInBytes => 8;

		[FieldOffset(0)] internal readonly byte Byte0;
		[FieldOffset(1)] internal readonly byte Byte1;
		[FieldOffset(2)] internal readonly byte Byte2;
		[FieldOffset(3)] internal readonly byte Byte3;
		[FieldOffset(4)] internal readonly byte Byte4;
		[FieldOffset(5)] internal readonly byte Byte5;
		[FieldOffset(6)] internal readonly byte Byte6;
		[FieldOffset(7)] internal readonly byte Byte7;

		[field: FieldOffset(0)] public float U { get; }
		[field: FieldOffset(4)] public float V { get; }

		public VertexAttribUv(float u, float v) {
			U = u;
			V = v;
		}

		public void Collect(ref byte[] arr, int index) {
			arr[index + 0] = Byte0;
			arr[index + 1] = Byte1;
			arr[index + 2] = Byte2;
			arr[index + 3] = Byte3;
			arr[index + 4] = Byte4;
			arr[index + 5] = Byte5;
			arr[index + 6] = Byte6;
			arr[index + 7] = Byte7;
		}

		public override string ToString() => $"U: {U}, V: {V}";
	}
}