using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Test {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct TestVertex {
		public required float X { get; init; }
		public required float Y { get; init; }
		public required float R { get; init; }
		public required float G { get; init; }
		public required float B { get; init; }

		[SetsRequiredMembers]
		public TestVertex(float x, float y, float r, float g, float b) {
			X = x;
			Y = y;
			R = r;
			G = g;
			B = b;
		}

		public static unsafe VkVertexInputBindingDescription GetBindingDescription() => new() { binding = 0, stride = (uint)sizeof(TestVertex), inputRate = VkVertexInputRate.VertexInputRateVertex, };

		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions() => [
				new() { binding = 0, location = 0, format = VkFormat.FormatR32g32Sfloat, offset = 0, }, new() { binding = 0, location = 1, format = VkFormat.FormatR32g32b32Sfloat, offset = sizeof(float) * 2, },
		];
	}
}