using System.Numerics;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vertex {
	public readonly unsafe record struct VertexXyz {
		public float X { get; init; }
		public float Y { get; init; }
		public float Z { get; init; }

		public VertexXyz() { }

		public VertexXyz(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		public VertexXyz(Vector3 position) : this(position.X, position.Y, position.Z) { }

		public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(VertexXyz), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];
		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [ new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, ];
	}
}