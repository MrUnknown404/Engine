using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3.Client {
	public unsafe class VulkanWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSurfaceKHR Surface { get; }
		public SurfaceCapablePhysicalGpu SelectedGpu { get; }
		public LogicalGpu LogicalGpu { get; }

		private readonly VkInstance vkInstance;

		public VulkanWindow(VulkanGraphicsBackend graphicsBackend, VkInstance vkInstance, string title, uint width, uint height) : base(graphicsBackend, title, width, height) {
			this.vkInstance = vkInstance;

			Surface = CreateSurface(vkInstance, WindowHandle);
			Logger.Debug("Created surface");

			SurfaceCapablePhysicalGpu[] availableGpus = GetValidGpus(graphicsBackend.PhysicalGpus, Surface);
			if (availableGpus.Length == 0) { throw new Engine3VulkanException("Could not find any valid GPUs"); }
			Logger.Debug("Obtained surface capable GPUs");

			SelectedGpu = PickBestGpu(availableGpus, graphicsBackend.RateGpuSuitability);
			Logger.Debug($"- Selected Gpu: {SelectedGpu.Name}");

			LogicalGpu = SelectedGpu.CreateLogicalDevice(graphicsBackend.GetAllRequiredDeviceExtensions(), graphicsBackend.GetAllRequiredValidationLayers());
			Logger.Debug("Created logical gpu");
			Logger.Trace($"- Handle: {LogicalGpu.LogicalDevice.Handle:X}");
		}

		protected override void Cleanup() {
			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, Surface, null);
		}

		[MustUseReturnValue]
		private static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) {
			VkH.CheckIfSuccess(Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR surface), VulkanException.Reason.CreateSurface);
			return surface;
		}

		[MustUseReturnValue]
		private static SurfaceCapablePhysicalGpu[] GetValidGpus(PhysicalGpu[] physicalGpus, VkSurfaceKHR surface) {
			List<SurfaceCapablePhysicalGpu> gpus = new();

			foreach (PhysicalGpu physicalGpu in physicalGpus) {
				VkPhysicalDevice physicalDevice = physicalGpu.PhysicalDevice;

				if (!FindQueueFamilies(physicalDevice, surface, out uint? graphicsFamily, out uint? presentFamily, out uint? transferFamily)) { continue; }
				if (!SwapChain.QuerySupport(physicalDevice, surface, out _, out _, out _)) { continue; }

				gpus.Add(new(physicalGpu, new(graphicsFamily.Value, presentFamily.Value, transferFamily.Value)));
			}

			return gpus.ToArray();

			[MustUseReturnValue]
			static VkQueueFamilyProperties2[] GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice) {
				uint queueFamilyPropertyCount = 0;
				Vk.GetPhysicalDeviceQueueFamilyProperties2(physicalDevice, &queueFamilyPropertyCount, null);

				if (queueFamilyPropertyCount == 0) { return Array.Empty<VkQueueFamilyProperties2>(); }

				VkQueueFamilyProperties2[] queueFamilyProperties2 = new VkQueueFamilyProperties2[queueFamilyPropertyCount];
				for (int i = 0; i < queueFamilyPropertyCount; i++) { queueFamilyProperties2[i] = new() { sType = VkStructureType.StructureTypeQueueFamilyProperties2, }; }

				fixed (VkQueueFamilyProperties2* queueFamilyPropertiesPtr = queueFamilyProperties2) {
					Vk.GetPhysicalDeviceQueueFamilyProperties2(physicalDevice, &queueFamilyPropertyCount, queueFamilyPropertiesPtr);
					return queueFamilyProperties2;
				}
			}

			[MustUseReturnValue]
			static bool FindQueueFamilies(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, [NotNullWhen(true)] out uint? graphicsFamily, [NotNullWhen(true)] out uint? presentFamily,
				[NotNullWhen(true)] out uint? transferFamily) {
				graphicsFamily = null;
				presentFamily = null;
				transferFamily = null;

				VkQueueFamilyProperties2[] queueFamilyProperties2 = GetPhysicalDeviceQueueFamilyProperties(physicalDevice);

				for (uint i = 0; i < queueFamilyProperties2.Length; i++) {
					VkQueueFamilyProperties queueFamilyProperties1 = queueFamilyProperties2[i].queueFamilyProperties;
					if ((queueFamilyProperties1.queueFlags & VkQueueFlagBits.QueueGraphicsBit) != 0) { graphicsFamily = i; }
					if ((queueFamilyProperties1.queueFlags & VkQueueFlagBits.QueueTransferBit) != 0) { transferFamily = i; }

					int presentSupport;
					Vk.GetPhysicalDeviceSurfaceSupportKHR(physicalDevice, i, surface, &presentSupport);
					if (presentSupport == Vk.True) { presentFamily = i; }

					if (graphicsFamily != null && presentFamily != null && transferFamily != null) { return true; }
				}

				return false;
			}
		}

		[MustUseReturnValue]
		private static SurfaceCapablePhysicalGpu PickBestGpu(SurfaceCapablePhysicalGpu[] physicalGpus, VulkanGraphicsBackend.RateGpuSuitabilityDelegate rateGpuSuitability) {
			SurfaceCapablePhysicalGpu? bestDevice = null;
			int bestDeviceScore = int.MinValue;

			foreach (SurfaceCapablePhysicalGpu device in physicalGpus) {
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