using System.Diagnostics;
using Engine3.Exceptions;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3.Graphics.Vulkan {
	public class VkWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSurfaceKHR VkSurface { get; }
		public PhysicalGpu[] AvailableGpus { get; }
		public PhysicalGpu SelectedGpu { get; }
		public LogicalGpu LogicalGpu { get; }
		public SwapChain SwapChain { get; }

		public VkPipelineLayout? VkPipelineLayout { get; private set; } // i'm pretty sure these are gonna need to be reworked later
		public VkPipeline? VkGraphicsPipeline { get; private set; }

		public VkCommandPool? VkCommandPool { get; private set; }
		public VkCommandBuffer? VkCommandBuffer { get; private set; }

		public VkSemaphore? VkImageAvailable { get; private set; }
		public VkSemaphore? VkRenderFinished { get; private set; }
		public VkFence? VkInFlight { get; private set; }

		private VkWindow(WindowHandle windowHandle, VkSurfaceKHR vkSurface, PhysicalGpu[] availableGpus, PhysicalGpu selectedGpu, LogicalGpu logicalGpu, SwapChain swapChain) : base(windowHandle) {
			VkSurface = vkSurface;
			AvailableGpus = availableGpus;
			SelectedGpu = selectedGpu;
			LogicalGpu = logicalGpu;
			SwapChain = swapChain;
		}

		public void CreateGraphicsPipeline(VkPipelineShaderStageCreateInfo[] vkShaderStageCreateInfos) {
			if (VkGraphicsPipeline != null) { throw new NotImplementedException(); } // TODO impl

			VkH.CreateGraphicsPipeline(LogicalGpu.VkLogicalDevice, SwapChain.VkImageFormat, vkShaderStageCreateInfos, out VkPipeline vkGraphicsPipeline, out VkPipelineLayout vkPipelineLayout);
			VkGraphicsPipeline = vkGraphicsPipeline;
			VkPipelineLayout = vkPipelineLayout;
			Logger.Debug("Created graphics pipeline");
		}

		public void CreateCommandPool() {
			VkCommandPool = VkH.CreateCommandPool(LogicalGpu.VkLogicalDevice, SelectedGpu.QueueFamilyIndices);
			Logger.Debug("Created command pool");
		}

		public void CreateCommandBuffer() {
			if (VkCommandPool is not { } vkCommandPool) { throw new VulkanException("Cannot create command buffer before the command pool is created"); }

			VkCommandBuffer = VkH.CreateCommandBuffer(LogicalGpu.VkLogicalDevice, vkCommandPool);
			Logger.Debug("Created command buffer");
		}

		public void CreateSyncObjects() {
			VkImageAvailable = VkH.CreateSemaphore(LogicalGpu.VkLogicalDevice);
			VkRenderFinished = VkH.CreateSemaphore(LogicalGpu.VkLogicalDevice);
			VkInFlight = VkH.CreateFence(LogicalGpu.VkLogicalDevice);
		}

		internal static VkWindow MakeVkWindow(WindowHandle windowHandle) {
			if (Engine3.VkInstance is not { } vkInstance || Engine3.GameInstance is not { } gameInstance) { throw new UnreachableException(); }

			VkSurfaceKHR vkSurface = VkH.CreateSurface(vkInstance, windowHandle);
			Logger.Debug("Created surface");

			PhysicalGpu[] availableGpus = VkH.GetValidGpus(Engine3.VkPhysicalDevices, vkSurface, gameInstance.VkIsPhysicalDeviceSuitable);
			if (availableGpus.Length == 0) { throw new VulkanException("Could not find any GPUs"); }
			Logger.Debug("Obtained surface capable GPUs ");
			VkH.PrintGpus(availableGpus, Engine3.Debug);

			PhysicalGpu? selectedGpu = VkH.PickBestGpu(availableGpus, gameInstance.VkRateGpuSuitability);
			if (selectedGpu == null) { throw new VulkanException("Could not find any suitable GPUs"); }
			Logger.Debug($"- Selected Gpu: {selectedGpu.Name}");

			VkDevice vkLogicalDevice = VkH.CreateLogicalDevice(selectedGpu);
			Logger.Debug("Created logical device");

			VkQueue vkGraphicsQueue = VkH.GetDeviceQueue(vkLogicalDevice, selectedGpu.QueueFamilyIndices.GraphicsFamily);
			Logger.Debug("Obtained graphics queue");

			VkQueue vkPresentQueue = VkH.GetDeviceQueue(vkLogicalDevice, selectedGpu.QueueFamilyIndices.PresentFamily);
			Logger.Debug("Obtained present queue");

			VkH.CreateSwapChain(windowHandle, vkSurface, selectedGpu, vkLogicalDevice, out VkSwapchainKHR vkSwapChain, out VkFormat vkSwapChainImageFormat, out VkExtent2D vkSwapChainExtent);
			Logger.Debug("Created swap chain");

			VkImage[] vkSwapChainImages = VkH.GetSwapChainImages(vkLogicalDevice, vkSwapChain);
			Logger.Debug("Obtained swap chain images");

			VkImageView[] vkSwapChainImageViews = VkH.CreateImageViews(vkLogicalDevice, vkSwapChainImages, vkSwapChainImageFormat);
			Logger.Debug("Created swap chain image views");

			return new(windowHandle, vkSurface, availableGpus, selectedGpu, new(vkLogicalDevice, vkGraphicsQueue, vkPresentQueue),
				new(vkSwapChain, vkSwapChainImages, vkSwapChainImageFormat, vkSwapChainExtent, vkSwapChainImageViews));
		}

		protected override unsafe void CleanupGraphics() {
			if (Engine3.VkInstance is not { } vkInstance) { return; }

			if (VkCommandPool is { } vkCommandPool) { Vk.DestroyCommandPool(LogicalGpu.VkLogicalDevice, vkCommandPool, null); }

			SwapChain.Destroy(LogicalGpu.VkLogicalDevice);

			if (VkPipelineLayout is { } vkPipelineLayout) { Vk.DestroyPipelineLayout(LogicalGpu.VkLogicalDevice, vkPipelineLayout, null); }
			if (VkGraphicsPipeline is { } vkGraphicsPipeline) { Vk.DestroyPipeline(LogicalGpu.VkLogicalDevice, vkGraphicsPipeline, null); }

			if (VkImageAvailable is { } vkImageAvailable) { Vk.DestroySemaphore(LogicalGpu.VkLogicalDevice, vkImageAvailable, null); }
			if (VkRenderFinished is { } vkRenderFinished) { Vk.DestroySemaphore(LogicalGpu.VkLogicalDevice, vkRenderFinished, null); }
			if (VkInFlight is { } vkInFlight) { Vk.DestroyFence(LogicalGpu.VkLogicalDevice, vkInFlight, null); }

			Vk.DeviceWaitIdle(LogicalGpu.VkLogicalDevice);

			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, VkSurface, null);
		}
	}
}