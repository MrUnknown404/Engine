using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Client.Graphics.Vulkan;

public interface IVulkanVertex {
	public static abstract VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0);
	public static abstract VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0);
}