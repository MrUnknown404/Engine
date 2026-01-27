using System.Diagnostics.CodeAnalysis;
using System.Text;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client {
	public unsafe class VkWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSurfaceKHR Surface { get; }
		public PhysicalGpu SelectedGpu { get; }
		public LogicalGpu LogicalGpu { get; }

		private readonly VkInstance vkInstance;

		public VkWindow(VulkanGraphicsBackend graphicsBackend, VkInstance vkInstance, string title, uint width, uint height) : base(graphicsBackend, title, width, height) {
			this.vkInstance = vkInstance;

			Surface = VkH.CreateSurface(vkInstance, WindowHandle);
			Logger.Debug("Created surface");

			PhysicalGpu[] availableGpus = GetValidGpus(graphicsBackend.PhysicalDevices, Surface, graphicsBackend.IsPhysicalDeviceSuitable, graphicsBackend.GetAllRequiredDeviceExtensions());
			if (availableGpus.Length == 0) { throw new Engine3VulkanException("Could not find any valid GPUs"); }
			Logger.Debug("Obtained surface capable GPUs");
			PrintGpus(availableGpus, Engine3.Debug);

			SelectedGpu = PickBestGpu(availableGpus, graphicsBackend.RateGpuSuitability);
			Logger.Debug($"- Selected Gpu: {SelectedGpu.Name}");

			LogicalGpu = new(SelectedGpu, graphicsBackend.GetAllRequiredDeviceExtensions(), graphicsBackend.GetAllRequiredValidationLayers());
			Logger.Debug("Created logical gpu");
		}

		protected override void Cleanup() {
			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, Surface, null);
		}

		[MustUseReturnValue]
		public static PhysicalGpu[] GetValidGpus(VkPhysicalDevice[] physicalDevices, VkSurfaceKHR surface, VulkanGraphicsBackend.IsPhysicalDeviceSuitableDelegate isPhysicalDeviceSuitable, string[] requiredDeviceExtensions) {
			List<PhysicalGpu> gpus = new();

			foreach (VkPhysicalDevice physicalDevice in physicalDevices) {
				VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new();
				VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new();
				Vk.GetPhysicalDeviceProperties2(physicalDevice, &physicalDeviceProperties2);
				Vk.GetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

				if (!isPhysicalDeviceSuitable(physicalDeviceProperties2.properties, physicalDeviceFeatures2.features)) { continue; }

				VkExtensionProperties[] physicalDeviceExtensionProperties = GetPhysicalDeviceExtensionProperties(physicalDevice);
				if (physicalDeviceExtensionProperties.Length == 0) { continue; }
				if (!CheckDeviceExtensionSupport(physicalDeviceExtensionProperties, requiredDeviceExtensions)) { continue; }
				if (!FindQueueFamilies(physicalDevice, surface, out uint? graphicsFamily, out uint? presentFamily, out uint? transferFamily)) { continue; }
				if (!SwapChain.QuerySupport(physicalDevice, surface, out _, out _, out _)) { continue; }

				gpus.Add(new(physicalDevice, physicalDeviceProperties2, physicalDeviceFeatures2, physicalDeviceExtensionProperties, new(graphicsFamily.Value, presentFamily.Value, transferFamily.Value)));
			}

			return gpus.ToArray();

			[MustUseReturnValue]
			static VkExtensionProperties[] GetPhysicalDeviceExtensionProperties(VkPhysicalDevice physicalDevice) {
				uint extensionCount;
				Vk.EnumerateDeviceExtensionProperties(physicalDevice, null, &extensionCount, null);

				if (extensionCount == 0) { return Array.Empty<VkExtensionProperties>(); }

				VkExtensionProperties[] physicalDeviceExtensionProperties = new VkExtensionProperties[extensionCount];
				fixed (VkExtensionProperties* extensionPropertiesPtr = physicalDeviceExtensionProperties) {
					Vk.EnumerateDeviceExtensionProperties(physicalDevice, null, &extensionCount, extensionPropertiesPtr);
					return physicalDeviceExtensionProperties;
				}
			}

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
			static bool CheckDeviceExtensionSupport(VkExtensionProperties[] physicalDeviceExtensionProperties, string[] requiredDeviceExtensions) =>
					requiredDeviceExtensions.All(wantedExtension => physicalDeviceExtensionProperties.Any(extensionProperties => {
						ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
						return Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension;
					}));

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
		private static PhysicalGpu PickBestGpu(PhysicalGpu[] physicalGpus, VulkanGraphicsBackend.RateGpuSuitabilityDelegate rateGpuSuitability) {
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