using System.Diagnostics;
using Engine3.Exceptions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics.Vulkan {
	public class VkWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSurfaceKHR Surface { get; }
		public PhysicalGpu SelectedGpu { get; }
		public LogicalGpu LogicalGpu { get; }
		public SwapChain SwapChain { get; }

		private VkWindow(WindowHandle windowHandle, VkSurfaceKHR surface, PhysicalGpu selectedGpu, LogicalGpu logicalGpu, SwapChain swapChain) : base(windowHandle) {
			Surface = surface;
			SelectedGpu = selectedGpu;
			LogicalGpu = logicalGpu;
			SwapChain = swapChain;
		}

		internal static VkWindow MakeVkWindow(GameClient gameClient, WindowHandle windowHandle) {
			if (Engine3.VkInstance is not { } vkInstance || Engine3.GameInstance is not { } gameInstance) { throw new UnreachableException(); }

			VkSurfaceKHR surface = VkH.CreateSurface(vkInstance, windowHandle);
			Logger.Debug("Created surface");

			PhysicalGpu[] availableGpus = VkH.GetValidGpus(Engine3.VkPhysicalDevices, surface, gameInstance.VkIsPhysicalDeviceSuitable, gameClient.VkGetDeviceExtensions());
			if (availableGpus.Length == 0) { throw new VulkanException("Could not find any valid GPUs"); }
			Logger.Debug("Obtained surface capable GPUs ");
			VkH.PrintGpus(availableGpus, Engine3.Debug);

			PhysicalGpu? selectedGpu = VkH.PickBestGpu(availableGpus, gameInstance.VkRateGpuSuitability);
			if (selectedGpu == null) { throw new VulkanException("Could not find any suitable GPUs"); }
			Logger.Debug($"- Selected Gpu: {selectedGpu.Name}");

			VkDevice logicalDevice = VkH.CreateLogicalDevice(selectedGpu.PhysicalDevice, selectedGpu.QueueFamilyIndices, gameClient.VkGetDeviceExtensions(), gameClient.VkGetRequiredValidationLayers());
			VkQueue graphicsQueue = VkH.GetDeviceQueue(logicalDevice, selectedGpu.QueueFamilyIndices.GraphicsFamily);
			VkQueue presentQueue = VkH.GetDeviceQueue(logicalDevice, selectedGpu.QueueFamilyIndices.PresentFamily);
			VkQueue transferQueue = VkH.GetDeviceQueue(logicalDevice, selectedGpu.QueueFamilyIndices.TransferFamily);
			LogicalGpu logicalGpu = new(logicalDevice, graphicsQueue, presentQueue, transferQueue);
			Logger.Debug("Created logical gpu");

			Toolkit.Window.GetFramebufferSize(windowHandle, out Vector2i framebufferSize);
			SwapChain swapChain = VkH.CreateSwapChain(selectedGpu, logicalDevice, surface, framebufferSize, VkPresentModeKHR.PresentModeImmediateKhr);
			Logger.Debug("Created swap chain");

			VkWindow window = new(windowHandle, surface, selectedGpu, logicalGpu, swapChain);
			swapChain.Window = window;

			return window;
		}

		protected override unsafe void CleanupGraphics() {
			if (Engine3.VkInstance is not { } vkInstance) { return; }

			SwapChain.Destroy();

			Vk.DeviceWaitIdle(LogicalGpu.LogicalDevice);

			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, Surface, null);
		}
	}
}