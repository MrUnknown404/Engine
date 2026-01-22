using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class LogicalGpu {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkDevice LogicalDevice { get; }
		public VkQueue GraphicsQueue { get; }
		public VkQueue PresentQueue { get; }
		public VkQueue TransferQueue { get; }

		private bool wasDestroyed;

		public LogicalGpu(VkDevice logicalDevice, VkQueue graphicsQueue, VkQueue presentQueue, VkQueue transferQueue) {
			LogicalDevice = logicalDevice;
			GraphicsQueue = graphicsQueue;
			PresentQueue = presentQueue;
			TransferQueue = transferQueue;
		}

		public unsafe void Destroy() {
			if (wasDestroyed) {
				Logger.Warn($"{nameof(LogicalGpu)} was already destroyed");
				return;
			}

			Vk.DestroyDevice(LogicalDevice, null);

			wasDestroyed = true;
		}
	}
}