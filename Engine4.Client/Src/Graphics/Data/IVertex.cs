using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Data;

public unsafe interface IVertex {
	public static abstract VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding);
	public static abstract VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding);

	protected static VkVertexInputBindingDescription[] CreateBindingDescriptions<T>(uint binding, VkVertexInputRate vertexInputRate)
			where T : unmanaged, IVertex => [ new() { binding = binding, stride = (uint)sizeof(T), inputRate = vertexInputRate, }, ];

	protected static VkVertexInputAttributeDescription[] CreateAttributeDescriptions(uint binding, (VkFormat, byte)[] formats) {
		if (formats.Length == 0) { throw new ArgumentException("Formats cannot be empty", nameof(formats)); }

		VkVertexInputAttributeDescription[] attributeDescriptions = new VkVertexInputAttributeDescription[formats.Length];
		uint offset = 0;

		for (uint i = 0; i < formats.Length; i++) {
			(VkFormat format, byte formatSize) = formats[i];
			attributeDescriptions[i] = new() { location = i, binding = binding, format = format, offset = offset, };
			offset += formatSize; // i'd set up a switch statement for size but why are there so many formats?? https://docs.vulkan.org/spec/latest/chapters/formats.html#formats-properties
		}

		return attributeDescriptions;
	}
}