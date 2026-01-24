using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public unsafe class VkWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSurfaceKHR Surface { get; }
		public PhysicalGpu SelectedGpu { get; }
		public LogicalGpu LogicalGpu { get; }

		public VkWindow(GameClient gameClient, VkInstance vkInstance, string title, uint width, uint height) : base(gameClient, title, width, height) {
			Surface = VkH.CreateSurface(vkInstance, WindowHandle);
			Logger.Debug("Created surface");

			PhysicalGpu[] availableGpus = VkH.GetValidGpus(gameClient.PhysicalDevices, Surface, gameClient.IsPhysicalDeviceSuitable, gameClient.GetAllRequiredDeviceExtensions());
			if (availableGpus.Length == 0) { throw new Engine3VulkanException("Could not find any valid GPUs"); }
			Logger.Debug("Obtained surface capable GPUs");
			PrintGpus(availableGpus, Engine3.Debug);

			SelectedGpu = PickBestGpu(availableGpus, gameClient.RateGpuSuitability);
			Logger.Debug($"- Selected Gpu: {SelectedGpu.Name}");

			VkDevice logicalDevice = VkH.CreateLogicalDevice(SelectedGpu.PhysicalDevice, SelectedGpu.QueueFamilyIndices, gameClient.GetAllRequiredDeviceExtensions(), gameClient.GetAllRequiredValidationLayers());
			VkQueue graphicsQueue = VkH.GetDeviceQueue(logicalDevice, SelectedGpu.QueueFamilyIndices.GraphicsFamily);
			VkQueue presentQueue = VkH.GetDeviceQueue(logicalDevice, SelectedGpu.QueueFamilyIndices.PresentFamily);
			VkQueue transferQueue = VkH.GetDeviceQueue(logicalDevice, SelectedGpu.QueueFamilyIndices.TransferFamily);
			LogicalGpu = new(logicalDevice, graphicsQueue, presentQueue, transferQueue);
			Logger.Debug("Created logical gpu");
		}

		protected override void Cleanup() {
			if (Engine3.GameInstance.VkInstance is not { } vkInstance) { return; }

			Vk.DeviceWaitIdle(LogicalGpu.LogicalDevice);

			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, Surface, null);
		}

		private static void PrintGpus(PhysicalGpu[] physicalGpus, bool verbose) {
			Logger.Debug("The following GPUs are available:");
			foreach (PhysicalGpu gpu in physicalGpus) {
				if (verbose) {
					foreach (string str in gpu.GetVerboseDescription()) { Logger.Debug(str); }
				} else {
					Logger.Debug($"- {gpu.GetSimpleDescription()}"); //
				}
			}
		}

		[MustUseReturnValue]
		public static PhysicalGpu PickBestGpu(PhysicalGpu[] physicalGpus, GameClient.RateGpuSuitabilityDelegate rateGpuSuitability) {
			PhysicalGpu? bestDevice = null;
			int bestDeviceScore = int.MinValue;

			foreach (PhysicalGpu device in physicalGpus) {
				int score = rateGpuSuitability(device);
				if (score > bestDeviceScore) {
					bestDevice = device;
					bestDeviceScore = score;
				}
			}

			return bestDevice ?? throw new Engine3VulkanException("Failed to find any gpus");
		}
	}
}