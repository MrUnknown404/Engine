using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Engine3.Shapes {
	[PublicAPI]
	public readonly record struct Rectangle {
		public required float Width { get; init; }
		public required float Height { get; init; }

		public float Area => Width * Height;

		[SetsRequiredMembers]
		public Rectangle(float width, float height) {
			if (width <= 0) { throw new ArgumentException("Width cannot be below or equal to zero"); }
			if (height <= 0) { throw new ArgumentException("Height cannot be below or equal to zero"); }

			Width = width;
			Height = height;
		}

		public Vector3[] ToVertices(float x, float y, float z) => [ new(x, y, z), new(x + Width, y, z), new(x, y + Height, z), new(x + Width, y + Height, z), ];

		public byte[] ToVertices(float x, float y, float z, ReadOnlySpan<byte> inputData, uint inputDataSize) {
			const byte VectorSize = 3 * sizeof(float);
			int totalVertexSize = (int)(VectorSize + inputDataSize);

			byte[] output = new byte[VectorSize * 4 + inputData.Length];
			ReadOnlySpan<byte> floatsAsBytes = MemoryMarshal.AsBytes([ x, y, z, x + Width, y, z, x, y + Height, z, x + Width, y + Height, z, ]);

			for (int i = 0; i < output.Length; i++) {
				int whichVertex = i / totalVertexSize;
				int vertexPosition = i % totalVertexSize;

				output[i] = vertexPosition < VectorSize ? floatsAsBytes[whichVertex * VectorSize + vertexPosition] : inputData[(int)(whichVertex * inputDataSize + (vertexPosition - VectorSize))];
			}

			return output;
		}
	}
}