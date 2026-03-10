using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.VertexLayouts {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct VertexXyzUvRgb {
		public float X { get; init; }
		public float Y { get; init; }
		public float Z { get; init; }

		public float U { get; init; }
		public float V { get; init; }

		public float R { get; init; }
		public float G { get; init; }
		public float B { get; init; }

		public VertexXyzUvRgb() { }

		public VertexXyzUvRgb(float x, float y, float z, float u, float v, float r, float g, float b) {
			X = x;
			Y = y;
			Z = z;
			U = u;
			V = v;
			R = r;
			G = g;
			B = b;
		}

		public VertexXyzUvRgb(Vector3 position, Vector2 uvs, Vector3 color) : this(position.X, position.Y, position.Z, uvs.X, uvs.Y, color.X, color.Y, color.Z) { }

		public static unsafe VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [
				new() { binding = binding, stride = (uint)sizeof(VertexXyzUvRgb), inputRate = VkVertexInputRate.VertexInputRateVertex, },
		];

		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
				new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
				new() { binding = binding, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 3, }, //
				new() { binding = binding, location = 2, format = VkFormat.FormatR32g32b32Sfloat, offset = sizeof(float) * 5, },
		];
	}
}