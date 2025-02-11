using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models.Vertex {
	[StructLayout(LayoutKind.Sequential)]
	public readonly record struct VertexLayout {
		public VertexAttribPointerType VertexAttribType { get; }
		public byte ElementCount { get; }

		public byte TypeToByteCount =>
				VertexAttribType switch {
						VertexAttribPointerType.Byte or VertexAttribPointerType.UnsignedByte => 1,
						VertexAttribPointerType.Short or VertexAttribPointerType.UnsignedShort or VertexAttribPointerType.HalfFloat => 2,
						VertexAttribPointerType.Int or VertexAttribPointerType.UnsignedInt or VertexAttribPointerType.Float or VertexAttribPointerType.Fixed => 4,
						VertexAttribPointerType.Double => 8,
						VertexAttribPointerType.UnsignedInt2101010Rev or VertexAttribPointerType.UnsignedInt10F11F11FRev or VertexAttribPointerType.Int2101010Rev => //
								throw new NotImplementedException("i have no idea what these are and i probably won't use them so im just gonna throw an exception."),
						_ => throw new ArgumentOutOfRangeException(),
				};

		public VertexLayout(VertexAttribPointerType vertexAttribType, byte elementCount) {
			if (elementCount > 4) { throw new ArgumentOutOfRangeException($"ElementCount must be [1..4]. Was {elementCount}"); }

			VertexAttribType = vertexAttribType;
			ElementCount = elementCount;
		}
	}
}