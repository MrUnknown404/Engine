using System.Reflection;
using Engine3.Graphics.Vulkan;
using Engine3.Utils;
using OpenTK.Graphics.Vulkan;

namespace Engine3 {
	public abstract class GameClient {
		public Assembly Assembly { get; internal init; } = null!; // Set in Engine3#Start

		public abstract Version4 Version { get; }

		protected internal abstract void Setup();

		protected internal abstract void Update();
		protected internal abstract void Render(float delta);

		protected internal virtual bool VkIsGpuSuitable(Gpu gpu) {
			VkPhysicalDeviceProperties deviceProperties = gpu.VkPhysicalDeviceProperties2.properties;
			return deviceProperties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu;
		}

		protected internal virtual int VkRateGpuSuitability(Gpu gpu) {
			VkPhysicalDeviceProperties deviceProperties = gpu.VkPhysicalDeviceProperties2.properties;
			int score = 0;

			if (deviceProperties.deviceType == VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu) { score += 1000; }
			score += (int)deviceProperties.limits.maxImageDimension2D;

			return score;
		}
	}
}