using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Graphics.Vulkan.Objects;
using JetBrains.Annotations;
using NLog;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3.Graphics.Vulkan {
	public unsafe class VkWindow : Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkSurfaceKHR Surface { get; }
		public PhysicalGpu SelectedGpu { get; }
		public LogicalGpu LogicalGpu { get; }

		public VkWindow(GameClient gameClient, VkInstance vkInstance, string title, uint width, uint height) : base(gameClient, title, width, height) {
			Surface = CreateSurface(vkInstance, WindowHandle);
			Logger.Debug("Created surface");

			PhysicalGpu[] availableGpus = GetValidGpus(gameClient.PhysicalDevices, Surface, gameClient.IsPhysicalDeviceSuitable, gameClient.GetAllRequiredDeviceExtensions());
			if (availableGpus.Length == 0) { throw new VulkanException("Could not find any valid GPUs"); }
			Logger.Debug("Obtained surface capable GPUs");
			PrintGpus(availableGpus, Engine3.Debug);

			SelectedGpu = PickBestGpu(availableGpus, gameClient.RateGpuSuitability);
			Logger.Debug($"- Selected Gpu: {SelectedGpu.Name}");

			VkDevice logicalDevice = CreateLogicalDevice(SelectedGpu.PhysicalDevice, SelectedGpu.QueueFamilyIndices, gameClient.GetAllRequiredDeviceExtensions(), gameClient.GetAllRequiredValidationLayers());
			VkQueue graphicsQueue = GetDeviceQueue(logicalDevice, SelectedGpu.QueueFamilyIndices.GraphicsFamily);
			VkQueue presentQueue = GetDeviceQueue(logicalDevice, SelectedGpu.QueueFamilyIndices.PresentFamily);
			VkQueue transferQueue = GetDeviceQueue(logicalDevice, SelectedGpu.QueueFamilyIndices.TransferFamily);
			LogicalGpu = new(logicalDevice, graphicsQueue, presentQueue, transferQueue);
			Logger.Debug("Created logical gpu");
		}

		protected override void Cleanup() {
			if (Engine3.GameInstance.VkInstance is not { } vkInstance) { return; }

			Vk.DeviceWaitIdle(LogicalGpu.LogicalDevice);

			LogicalGpu.Destroy();
			Vk.DestroySurfaceKHR(vkInstance, Surface, null);
		}

		[MustUseReturnValue]
		private static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) {
			VkResult result = Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR surface);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create surface. {result}") : surface;
		}

		[MustUseReturnValue]
		private static PhysicalGpu[] GetValidGpus(VkPhysicalDevice[] physicalDevices, VkSurfaceKHR surface, GameClient.IsPhysicalDeviceSuitableDelegate isPhysicalDeviceSuitable, string[] requiredDeviceExtensions) {
			List<PhysicalGpu> gpus = new();

			foreach (VkPhysicalDevice physicalDevice in physicalDevices) {
				VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new();
				VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new();
				Vk.GetPhysicalDeviceProperties2(physicalDevice, &physicalDeviceProperties2);
				Vk.GetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

				if (!isPhysicalDeviceSuitable(physicalDeviceProperties2, physicalDeviceFeatures2)) { continue; }

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
		private static VkDevice CreateLogicalDevice(VkPhysicalDevice physicalDevice, QueueFamilyIndices queueFamilyIndices, string[] requiredDeviceExtensions, string[] requiredValidationLayers) {
			List<VkDeviceQueueCreateInfo> queueCreateInfos = new();
			float queuePriority = 1f;

			// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator // CS0212
			foreach (uint queueFamily in new HashSet<uint> { queueFamilyIndices.GraphicsFamily, queueFamilyIndices.PresentFamily, queueFamilyIndices.TransferFamily, }) {
				queueCreateInfos.Add(new() { queueFamilyIndex = queueFamily, queueCount = 1, pQueuePriorities = &queuePriority, });
			}

			VkPhysicalDeviceVulkan11Features physicalDeviceVulkan11Features = new(); // atm i'm not using most of these. but i may?
			VkPhysicalDeviceVulkan12Features physicalDeviceVulkan12Features = new() { pNext = &physicalDeviceVulkan11Features, };
			VkPhysicalDeviceVulkan13Features physicalDeviceVulkan13Features = new() { pNext = &physicalDeviceVulkan12Features, synchronization2 = (int)Vk.True, dynamicRendering = (int)Vk.True, };
			VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = new() { pNext = &physicalDeviceVulkan13Features, };
			VkPhysicalDeviceFeatures deviceFeatures = new(); // ???

			IntPtr requiredDeviceExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredDeviceExtensions);
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredValidationLayers);
#endif

			fixed (VkDeviceQueueCreateInfo* queueCreateInfosPtr = CollectionsMarshal.AsSpan(queueCreateInfos)) {
				VkDeviceCreateInfo deviceCreateInfo = new() {
						pNext = &physicalDeviceVulkan14Features,
						pQueueCreateInfos = queueCreateInfosPtr,
						queueCreateInfoCount = (uint)queueCreateInfos.Count,
						pEnabledFeatures = &deviceFeatures,
						ppEnabledExtensionNames = (byte**)requiredDeviceExtensionsPtr,
						enabledExtensionCount = (uint)requiredDeviceExtensions.Length,
#if DEBUG // https://docs.vulkan.org/spec/latest/appendices/legacy.html#legacy-devicelayers
#pragma warning disable CS0618 // Type or member is obsolete
						enabledLayerCount = (uint)requiredValidationLayers.Length,
						ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				};

				VkDevice logicalDevice;
				VkResult result = Vk.CreateDevice(physicalDevice, &deviceCreateInfo, null, &logicalDevice);

				MarshalTk.FreeStringArrayCoTaskMem(requiredDeviceExtensionsPtr, requiredDeviceExtensions.Length);
#if DEBUG
				MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

				return result != VkResult.Success ? throw new VulkanException($"Failed to create logical device. {result}") : logicalDevice;
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

			return bestDevice ?? throw new VulkanException("Failed to find any gpus");
		}

		[MustUseReturnValue]
		private static VkQueue GetDeviceQueue(VkDevice logicalDevice, uint queueFamilyIndex) {
			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
			VkQueue queue;
			Vk.GetDeviceQueue2(logicalDevice, &deviceQueueInfo2, &queue);
			return queue;
		}
	}
}