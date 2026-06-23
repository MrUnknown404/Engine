using Engine3.Client.Graphics.OpenGL;
using Engine3.Client.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.VertexLayouts;

public readonly unsafe record struct VertexXyzUvNormal : IVulkanVertex, IOpenGLVertex {
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }

	public float U { get; init; }
	public float V { get; init; }

	public float NormalX { get; init; }
	public float NormalY { get; init; }
	public float NormalZ { get; init; }

	public VertexXyzUvNormal(float x, float y, float z, float u, float v, float normalX, float normalY, float normalZ) {
		X = x;
		Y = y;
		Z = z;
		U = u;
		V = v;
		NormalX = normalX;
		NormalY = normalY;
		NormalZ = normalZ;
	}

	public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(VertexXyzUvNormal), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

	public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
			new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
			new() { binding = binding, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 3, }, //
			new() { binding = binding, location = 2, format = VkFormat.FormatR32g32b32Sfloat, offset = sizeof(float) * 5, },
	];

	public override string ToString() => $"({X}, {Y}, {Z})";
}