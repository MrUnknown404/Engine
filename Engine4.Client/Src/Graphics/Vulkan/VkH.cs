using Engine4.Client.Utility.Exceptions;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public static class VkH {
	public const int True = (int)Vk.True;
	public const int False = (int)Vk.False;

	public static void CheckSuccess(VkResult result, string failedMessage) {
		if (result != VkResult.Success) { throw new VulkanException($"{failedMessage}. Reason: {result}"); }
	}
}