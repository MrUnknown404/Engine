using OpenTK.Graphics.OpenGL;

namespace Engine3.Graphics.Vertex {
	public readonly record struct VertexAttributeFormat {
		public VertexAttribType AttribType { get; }
		public byte ComponentCount { get; }

		public VertexAttributeFormat(VertexAttribType attribType, byte componentCount) {
			AttribType = attribType;
			ComponentCount = componentCount;
		}

		public byte AttribByteSize =>
				AttribType switch {
						VertexAttribType.Byte => 1,
						VertexAttribType.UnsignedByte => 1,
						VertexAttribType.Short => 2,
						VertexAttribType.UnsignedShort => 2,
						VertexAttribType.Int => 4,
						VertexAttribType.UnsignedInt => 4,
						VertexAttribType.Float => 4,
						VertexAttribType.Double => 8,
						VertexAttribType.HalfFloat => 2,
						VertexAttribType.Fixed => 4,
						VertexAttribType.UnsignedInt2101010Rev => throw new NotImplementedException(),
						VertexAttribType.UnsignedInt10f11f11fRev => throw new NotImplementedException(),
						VertexAttribType.Int2101010Rev => throw new NotImplementedException(),
						_ => throw new ArgumentOutOfRangeException(),
				};
	}
}