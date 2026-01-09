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
		public VkDevice VkLogicalDevice { get; } // TODO LogicalGpu class?
		public VkQueue VkGraphicsQueue { get; }
		public VkQueue VkPresentQueue { get; }
		public VkSwapchainKHR VkSwapChain { get; }
		public VkImage[] VkSwapChainImages { get; }
		public VkFormat VkSwapChainImageFormat { get; }
		public VkExtent2D VkSwapChainExtent { get; }
		public VkImageView[] VkSwapChainImageViews { get; }

		public VkPipelineLayout? VkPipelineLayout { get; private set; } // i'm pretty sure these are gonna need to be reworked later
		public VkPipeline? VkGraphicsPipeline { get; private set; }

		private VkWindow(WindowHandle windowHandle, VkSurfaceKHR vkSurface, PhysicalGpu[] availableGpus, PhysicalGpu selectedGpu, VkDevice vkLogicalDevice, VkQueue vkGraphicsQueue, VkQueue vkPresentQueue,
			VkSwapchainKHR vkSwapChain, VkImage[] vkSwapChainImages, VkFormat vkSwapChainImageFormat, VkExtent2D vkSwapChainExtent, VkImageView[] vkSwapChainImageViews) : base(windowHandle) {
			VkSurface = vkSurface;
			AvailableGpus = availableGpus;
			SelectedGpu = selectedGpu;
			VkLogicalDevice = vkLogicalDevice;
			VkGraphicsQueue = vkGraphicsQueue;
			VkPresentQueue = vkPresentQueue;
			VkSwapChain = vkSwapChain;
			VkSwapChainImages = vkSwapChainImages;
			VkSwapChainImageFormat = vkSwapChainImageFormat;
			VkSwapChainExtent = vkSwapChainExtent;
			VkSwapChainImageViews = vkSwapChainImageViews;
		}

		public void CreateGraphicsPipeline(VkPipelineShaderStageCreateInfo[] vkShaderStageCreateInfos) {
			if (VkGraphicsPipeline != null) { throw new NotImplementedException(); } // TODO impl

			VkH.CreateGraphicsPipeline(VkLogicalDevice, VkSwapChainImageFormat, vkShaderStageCreateInfos, out VkPipeline vkGraphicsPipeline, out VkPipelineLayout vkPipelineLayout);
			VkGraphicsPipeline = vkGraphicsPipeline;
			VkPipelineLayout = vkPipelineLayout;
			Logger.Debug("Created graphics pipeline");
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

			VkQueue vkPresentQueue = VkH.GetDeviceQueue(vkLogicalDevice, selectedGpu.QueueFamilyIndices.GraphicsFamily);
			Logger.Debug("Obtained present queue");

			VkQueue vkGraphicsQueue = VkH.GetDeviceQueue(vkLogicalDevice, selectedGpu.QueueFamilyIndices.GraphicsFamily);
			Logger.Debug("Obtained graphics queue");

			VkH.CreateSwapChain(windowHandle, vkSurface, selectedGpu, vkLogicalDevice, out VkSwapchainKHR vkSwapChain, out VkFormat vkSwapChainImageFormat, out VkExtent2D vkSwapChainExtent);
			Logger.Debug("Created swap chain");

			VkImage[] vkSwapChainImages = VkH.GetSwapChainImages(vkLogicalDevice, vkSwapChain);
			Logger.Debug("Obtained swap chain images");

			VkImageView[] vkSwapChainImageViews = VkH.CreateImageViews(vkLogicalDevice, vkSwapChainImages, vkSwapChainImageFormat);
			Logger.Debug("Created swap chain image views");

			return new(windowHandle, vkSurface, availableGpus, selectedGpu, vkLogicalDevice, vkGraphicsQueue, vkPresentQueue, vkSwapChain, vkSwapChainImages, vkSwapChainImageFormat, vkSwapChainExtent, vkSwapChainImageViews);
		}

		protected override unsafe void CleanupGraphics() {
			if (Engine3.VkInstance is not { } vkInstance) { return; }

			Vk.DestroySwapchainKHR(VkLogicalDevice, VkSwapChain, null);
			foreach (VkImageView vkImageView in VkSwapChainImageViews) { Vk.DestroyImageView(VkLogicalDevice, vkImageView, null); }

			if (VkPipelineLayout is { } vkPipelineLayout) { Vk.DestroyPipelineLayout(VkLogicalDevice, vkPipelineLayout, null); }
			if (VkGraphicsPipeline is { } vkGraphicsPipeline) { Vk.DestroyPipeline(VkLogicalDevice, vkGraphicsPipeline, null); }

			Vk.DestroyDevice(VkLogicalDevice, null);
			Vk.DestroySurfaceKHR(vkInstance, VkSurface, null);
		}
	}
}