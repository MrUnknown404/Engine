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
		public SwapChain SwapChain { get; private set; }

		public VkCommandPool VkGraphicsCommandPool { get; }
		public VkCommandPool VkTransferCommandPool { get; }

		public VkCommandBuffer[] VkGraphicsCommandBuffers { get; } // TODO should these be wrapped into their own class?
		public VkSemaphore[] VkImageAvailable { get; }
		public VkSemaphore[] VkRenderFinished { get; }
		public VkFence[] VkInFlights { get; }

		public VkPipelineLayout? VkPipelineLayout { get; private set; } // i'm pretty sure these are gonna need to be reworked later
		public VkPipeline? VkGraphicsPipeline { get; private set; }

		public uint CurrentFrame { get; internal set; }

		private VkWindow(WindowHandle windowHandle, VkSurfaceKHR vkSurface, PhysicalGpu selectedGpu, LogicalGpu logicalGpu, SwapChain swapChain, VkCommandPool vkGraphicsCommandPool, VkCommandPool vkTransferCommandPool,
			VkCommandBuffer[] vkGraphicsCommandBuffers, VkSemaphore[] vkImageAvailable, VkSemaphore[] vkRenderFinished, VkFence[] vkInFlights) : base(windowHandle) {
			VkSurface = vkSurface;
			SelectedGpu = selectedGpu;
			LogicalGpu = logicalGpu;
			SwapChain = swapChain;
			VkGraphicsCommandPool = vkGraphicsCommandPool;
			VkTransferCommandPool = vkTransferCommandPool;
			VkGraphicsCommandBuffers = vkGraphicsCommandBuffers;
			VkImageAvailable = vkImageAvailable;
			VkRenderFinished = vkRenderFinished;
			VkInFlights = vkInFlights;
		}

		public void CreateGraphicsPipeline(VkPipelineShaderStageCreateInfo[] vkShaderStageCreateInfos) {
			if (VkGraphicsPipeline != null) { throw new NotImplementedException(); } // TODO impl

			VkH.CreateGraphicsPipeline(LogicalGpu.VkLogicalDevice, SwapChain.VkImageFormat, vkShaderStageCreateInfos, out VkPipeline vkGraphicsPipeline, out VkPipelineLayout vkPipelineLayout);
			VkGraphicsPipeline = vkGraphicsPipeline;
			VkPipelineLayout = vkPipelineLayout;
			Logger.Debug("Created graphics pipeline");
		}

		public void RecreateSwapChain() { // TODO move into swapchain class
			SwapChain = VkH.RecreateSwapChain(WindowHandle, VkSurface, SelectedGpu, LogicalGpu.VkLogicalDevice, SwapChain);
			Logger.Debug("Recreated swap chain");
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

			SwapChain swapChain = VkH.CreateSwapChain(windowHandle, vkSurface, selectedGpu, vkLogicalDevice, VkPresentModeKHR.PresentModeImmediateKhr);
			Logger.Debug("Created swap chain");

			VkCommandPool vkGraphicsCommandPool = VkH.CreateCommandPool(vkLogicalDevice, selectedGpu.QueueFamilyIndices.GraphicsFamily);
			Logger.Debug("Created graphics command pool");

			VkCommandPool vkTransferCommandPool = VkH.CreateCommandPool(vkLogicalDevice, selectedGpu.QueueFamilyIndices.TransferFamily);
			Logger.Debug("Created transfer command pool");

			VkCommandBuffer[] vkCommandBuffers = VkH.CreateCommandBuffers(vkLogicalDevice, vkGraphicsCommandPool, gameClient.MaxFramesInFlight);
			Logger.Debug("Created command buffer");

			VkSemaphore[] vkImageAvailable = VkH.CreateSemaphores(vkLogicalDevice, gameClient.MaxFramesInFlight);
			VkSemaphore[] vkRenderFinished = VkH.CreateSemaphores(vkLogicalDevice, gameClient.MaxFramesInFlight);
			VkFence[] vkInFlight = VkH.CreateFence(vkLogicalDevice, gameClient.MaxFramesInFlight);
			Logger.Debug("Created sync objects");

			return new(windowHandle, vkSurface, selectedGpu, new(vkLogicalDevice, vkGraphicsQueue, vkPresentQueue), swapChain, vkGraphicsCommandPool, vkTransferCommandPool, vkCommandBuffers, vkImageAvailable, vkRenderFinished,
				vkInFlight);
		}

		protected override unsafe void CleanupGraphics() {
			if (Engine3.VkInstance is not { } vkInstance) { return; }

			Vk.DestroyCommandPool(LogicalGpu.VkLogicalDevice, VkTransferCommandPool, null);
			Vk.DestroyCommandPool(LogicalGpu.VkLogicalDevice, VkGraphicsCommandPool, null);

			SwapChain.Destroy(LogicalGpu.VkLogicalDevice);

			if (VkPipelineLayout is { } vkPipelineLayout) { Vk.DestroyPipelineLayout(LogicalGpu.VkLogicalDevice, vkPipelineLayout, null); }
			if (VkGraphicsPipeline is { } vkGraphicsPipeline) { Vk.DestroyPipeline(LogicalGpu.VkLogicalDevice, vkGraphicsPipeline, null); }

			foreach (VkSemaphore vkSemaphore in VkImageAvailable) { Vk.DestroySemaphore(LogicalGpu.VkLogicalDevice, vkSemaphore, null); }
			foreach (VkSemaphore vkSemaphore in VkRenderFinished) { Vk.DestroySemaphore(LogicalGpu.VkLogicalDevice, vkSemaphore, null); }
			foreach (VkFence vkFence in VkInFlights) { Vk.DestroyFence(LogicalGpu.VkLogicalDevice, vkFence, null); }

			Vk.DeviceWaitIdle(LogicalGpu.VkLogicalDevice);

			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, VkSurface, null);
		}
	}
}