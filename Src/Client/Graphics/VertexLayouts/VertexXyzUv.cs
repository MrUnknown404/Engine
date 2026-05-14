using System.Numerics;
using Engine3.Client.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.VertexLayouts;

public readonly record struct VertexXyzUv : IVulkanVertex {
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }

	public float U { get; init; }
	public float V { get; init; }

	public VertexXyzUv() { }

	public VertexXyzUv(float x, float y, float z, float u, float v) {
		X = x;
		Y = y;
		Z = z;
		U = u;
		V = v;
	}

	public VertexXyzUv(Vector3 position, Vector2 uvs) : this(position.X, position.Y, position.Z, uvs.X, uvs.Y) { }

	public static unsafe VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(VertexXyzUv), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

	public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
			new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
			new() { binding = binding, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 3, },
	];
}