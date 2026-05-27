using System.Numerics;
using Engine3.Client.Client.Graphics.OpenGL;
using Engine3.Client.Client.Graphics.Vulkan;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Client.Graphics.VertexLayouts;

public readonly unsafe record struct VertexXyz : IVulkanVertex, IOpenGLVertex {
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