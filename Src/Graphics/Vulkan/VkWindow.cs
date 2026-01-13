using System.Diagnostics;
using Engine3.Exceptions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3.Graphics.Vulkan {
	public class VkWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSurfaceKHR VkSurface { get; }
		public PhysicalGpu SelectedGpu { get; }
		public LogicalGpu LogicalGpu { get; }
		public SwapChain SwapChain { get; }

		private VkWindow(WindowHandle windowHandle, VkSurfaceKHR vkSurface, PhysicalGpu selectedGpu, LogicalGpu logicalGpu, SwapChain swapChain) : base(windowHandle) {
			VkSurface = vkSurface;
			SelectedGpu = selectedGpu;
			LogicalGpu = logicalGpu;
			SwapChain = swapChain;
		}

		internal static VkWindow MakeVkWindow(GameClient gameClient, WindowHandle windowHandle) {
			if (Engine3.VkInstance is not { } vkInstance || Engine3.GameInstance is not { } gameInstance) { throw new UnreachableException(); }

			VkSurfaceKHR vkSurface = VkH.CreateSurface(vkInstance, windowHandle);
			Logger.Debug("Created surface");

			PhysicalGpu[] availableGpus = VkH.GetValidGpus(Engine3.VkPhysicalDevices, vkSurface, gameInstance.VkIsPhysicalDeviceSuitable, gameClient.VkGetDeviceExtensions());
			if (availableGpus.Length == 0) { throw new VulkanException("Could not find any valid GPUs"); }
			Logger.Debug("Obtained surface capable GPUs ");
			VkH.PrintGpus(availableGpus, Engine3.Debug);

			PhysicalGpu? selectedGpu = VkH.PickBestGpu(availableGpus, gameInstance.VkRateGpuSuitability);
			if (selectedGpu == null) { throw new VulkanException("Could not find any suitable GPUs"); }
			Logger.Debug($"- Selected Gpu: {selectedGpu.Name}");

			VkDevice vkLogicalDevice = VkH.CreateLogicalDevice(selectedGpu, gameClient.VkGetDeviceExtensions(), gameClient.VkGetRequiredValidationLayers());
			Logger.Debug("Created logical device");

			VkQueue vkGraphicsQueue = VkH.GetDeviceQueue(vkLogicalDevice, selectedGpu.QueueFamilyIndices.GraphicsFamily);
			Logger.Debug("Obtained graphics queue");

			VkQueue vkPresentQueue = VkH.GetDeviceQueue(vkLogicalDevice, selectedGpu.QueueFamilyIndices.PresentFamily);
			Logger.Debug("Obtained present queue");

			VkQueue vkTransferQueue = VkH.GetDeviceQueue(vkLogicalDevice, selectedGpu.QueueFamilyIndices.TransferFamily);
			Logger.Debug("Obtained transfer queue");

			SwapChain swapChain = VkH.CreateSwapChain(windowHandle, vkSurface, selectedGpu, vkLogicalDevice, VkPresentModeKHR.PresentModeImmediateKhr);
			Logger.Debug("Created swap chain");

			return new(windowHandle, vkSurface, selectedGpu, new(vkLogicalDevice, vkGraphicsQueue, vkPresentQueue, vkTransferQueue), swapChain);
		}

		protected override unsafe void CleanupGraphics() {
			if (Engine3.VkInstance is not { } vkInstance) { return; }

			SwapChain.Destroy(LogicalGpu.VkLogicalDevice);

			Vk.DeviceWaitIdle(LogicalGpu.VkLogicalDevice);

			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, VkSurface, null);
		}
	}
}