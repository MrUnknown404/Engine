using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class LogicalGpu {
		public VkDevice VkLogicalDevice { get; }
		public VkQueue VkGraphicsQueue { get; }
		public VkQueue VkPresentQueue { get; }

		public LogicalGpu(VkDevice vkLogicalDevice, VkQueue vkGraphicsQueue, VkQueue vkPresentQueue) {
			VkLogicalDevice = vkLogicalDevice;
			VkGraphicsQueue = vkGraphicsQueue;
			VkPresentQueue = vkPresentQueue;
		}

		public unsafe void Destroy() => Vk.DestroyDevice(VkLogicalDevice, null);
	}
}