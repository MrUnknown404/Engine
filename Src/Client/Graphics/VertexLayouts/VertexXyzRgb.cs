using System.Numerics;
using Engine3.Client.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.VertexLayouts;

public readonly unsafe record struct VertexXyzRgb : IVulkanVertex {
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }

	public float R { get; init; }
	public float G { get; init; }
	public float B { get; init; }

	public VertexXyzRgb() { }

	public VertexXyzRgb(float x, float y, float z, float r, float g, float b) {
		X = x;
		Y = y;
		Z = z;
		R = r;
		G = g;
		B = b;
	}

	public VertexXyzRgb(Vector3 position, Vector3 color) : this(position.X, position.Y, position.Z, color.X, color.Y, color.Z) { }

	public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(VertexXyzRgb), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

	public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
			new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
			new() { binding = binding, location = 1, format = VkFormat.FormatR32g32b32Sfloat, offset = sizeof(float) * 3, },
	];
}