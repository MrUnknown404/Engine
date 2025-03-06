using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models.Vertex {
	[PublicAPI]
	[StructLayout(LayoutKind.Explicit)]
	public readonly record struct VertexXyz : IVertexLayout {
		public static VertexAttribLayout[] VertexLayout { get; } = { new(VertexAttribPointerType.Float, 3), };
		public static byte SizeInBytes => 12;

		[FieldOffset(0)] internal readonly byte Byte0;
		[FieldOffset(1)] internal readonly byte Byte1;
		[FieldOffset(2)] internal readonly byte Byte2;
		[FieldOffset(3)] internal readonly byte Byte3;
		[FieldOffset(4)] internal readonly byte Byte4;
		[FieldOffset(5)] internal readonly byte Byte5;
		[FieldOffset(6)] internal readonly byte Byte6;
		[FieldOffset(7)] internal readonly byte Byte7;
		[FieldOffset(8)] internal readonly byte Byte8;
		[FieldOffset(9)] internal readonly byte Byte9;
		[FieldOffset(10)] internal readonly byte Byte10;
		[FieldOffset(11)] internal readonly byte Byte11;

		[field: FieldOffset(0)] public float X { get; }
		[field: FieldOffset(4)] public float Y { get; }
		[field: FieldOffset(8)] public float Z { get; }

		public VertexXyz(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
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
			arr[index + 8] = Byte8;
			arr[index + 9] = Byte9;
			arr[index + 10] = Byte10;
			arr[index + 11] = Byte11;
		}

		public override string ToString() => $"X: {X}, Y: {Y}, Z: {Z}";
	}
}