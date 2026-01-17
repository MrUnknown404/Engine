using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class LogicalGpu {
		public VkDevice LogicalDevice { get; }
		public VkQueue GraphicsQueue { get; }
		public VkQueue PresentQueue { get; }
		public VkQueue TransferQueue { get; }

		public LogicalGpu(VkDevice logicalDevice, VkQueue graphicsQueue, VkQueue presentQueue, VkQueue transferQueue) {
			LogicalDevice = logicalDevice;
			GraphicsQueue = graphicsQueue;
			PresentQueue = presentQueue;
			TransferQueue = transferQueue;
		}

		public unsafe void Destroy() => Vk.DestroyDevice(LogicalDevice, null);
	}
}