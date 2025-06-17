using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client.Model.Mesh.Vertex {
	[StructLayout(LayoutKind.Sequential)]
	public readonly record struct VertexAttribute {
		public required VertexAttribType VertexAttribType { get; init; }
		public required byte ElementCount { get; init; }

		/// <summary> Size in bytes </summary>
		/// <remarks> Throws an exception when <see cref="VertexAttribType"/> is an unimplemented data type </remarks>
		public byte ElementByteSize =>
				VertexAttribType switch {
						VertexAttribType.Byte or VertexAttribType.UnsignedByte => 1,
						VertexAttribType.Short or VertexAttribType.UnsignedShort or VertexAttribType.HalfFloat => 2,
						VertexAttribType.Int or VertexAttribType.UnsignedInt or VertexAttribType.Float or VertexAttribType.Fixed => 4,
						VertexAttribType.Double => 8,
						VertexAttribType.UnsignedInt2101010Rev or VertexAttribType.UnsignedInt10F11F11FRev or VertexAttribType.Int2101010Rev => //
								throw new NotImplementedException("i have no idea what these are and i probably won't use them so im just gonna throw an exception. edit: i have learned what they are. i probably won't use them"),
						_ => throw new ArgumentOutOfRangeException(),
				};

		[SetsRequiredMembers]
		public VertexAttribute(VertexAttribType vertexAttribType, byte elementCount) {
			if (elementCount > 4) { throw new ArgumentOutOfRangeException($"ElementCount must be [1..4]. Was {elementCount}"); }

			VertexAttribType = vertexAttribType;
			ElementCount = elementCount;
		}
	}
}