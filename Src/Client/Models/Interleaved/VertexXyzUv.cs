// using System.Runtime.InteropServices;
// using OpenTK.Graphics.OpenGL4;
// // TODO impl
// namespace USharpLibs.Engine2.Client.Models.VertexNew {
// 	[PublicAPI]
// 	[StructLayout(LayoutKind.Explicit, Size = Size)]
// 	public readonly record struct VertexXyzUv : IVertexNew {
// 		private const byte Size = 5 * sizeof(float);
//
// 		public static VertexLayout[] VertexLayout { get; } = { new(VertexAttribPointerType.Float, 3), new(VertexAttribPointerType.Float, 2), };
// 		public static byte StructSizeInBytes => Size;
//
// 		[field: FieldOffset(0)] public byte[] Elements { get; } = new byte[Size];
// 		[field: FieldOffset(0)] public float X { get; init; }
// 		[field: FieldOffset(4)] public float Y { get; init; }
// 		[field: FieldOffset(8)] public float Z { get; init; }
// 		[field: FieldOffset(12)] public float U { get; init; }
// 		[field: FieldOffset(16)] public float V { get; init; }
//
// 		public VertexXyzUv(float x, float y, float z, float u, float v) {
// 			X = x;
// 			Y = y;
// 			Z = z;
// 			U = u;
// 			V = v;
// 		}
// 	}
// }