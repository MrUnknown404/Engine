using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models.Vertex {
	[StructLayout(LayoutKind.Explicit)]
	public readonly record struct VertexAttribRgba : IVertexAttribute {
		public static VertexAttribLayout VertexLayout { get; } = new(VertexAttribPointerType.UnsignedInt, 1);
		public static byte SizeInBytes => 4;

		[FieldOffset(0)] internal readonly byte Byte0;
		[FieldOffset(1)] internal readonly byte Byte1;
		[FieldOffset(2)] internal readonly byte Byte2;
		[FieldOffset(3)] internal readonly byte Byte3;

		[field: FieldOffset(0)] public uint Rgba { get; }

		public byte R => Byte0;
		public byte G => Byte1;
		public byte B => Byte2;
		public byte A => Byte3;

		public VertexAttribRgba(uint rgba) => Rgba = rgba;

		public VertexAttribRgba(byte r, byte b, byte g, byte a) {
			Byte0 = r;
			Byte1 = g;
			Byte2 = b;
			Byte3 = a;
		}

		public void Collect(ref byte[] arr, int index) {
			arr[index + 0] = Byte0;
			arr[index + 1] = Byte1;
			arr[index + 2] = Byte2;
			arr[index + 3] = Byte3;
		}

		public override string ToString() => $"Rgba: {Rgba:X8}";
	}
}