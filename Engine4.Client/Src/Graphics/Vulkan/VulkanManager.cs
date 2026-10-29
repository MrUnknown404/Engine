using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Utility;
using Engine4.Client.Utility.Exceptions;
using Engine4.IO;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Utils;

namespace Engine4.Client.Graphics.Vulkan;

public sealed unsafe class VulkanManager {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	// properties
	public readonly string[] RequiredInstanceLayerProperties;
	public readonly string[] RequiredInstanceExtensionProperties;
	public readonly string[] RequiredDeviceExtensionProperties;

	public VkPresentModeKHR PresentMode { get; } // TODO support setting this at runtime
	public bool AllowEnableAnisotropy { get; }

	public byte MaxFramesInFlight { get; } // move?

	// objects
	public VulkanInstance VulkanInstance { get; }

	private readonly PhysicalGpu[] physicalGpus;

#if DEBUG
	private readonly VulkanDebugMessenger debugMessenger;
#endif

	// other
	private readonly SelectGpuMode selectGpuMode;
	private readonly SelectGpuDelegate? getManualGpuFunc;
	private readonly RateGpuSuitabilityDelegate? rateGpuSuitability;

	internal VulkanManager(GameClient game, VulkanStartupSettings vulkanStartupSettings) {
		PresentMode = vulkanStartupSettings.PresentMode;
		AllowEnableAnisotropy = vulkanStartupSettings.AllowEnableAnisotropy;
		MaxFramesInFlight = vulkanStartupSettings.MaxFramesInFlight;

		selectGpuMode = vulkanStartupSettings.SelectGpuMode;
		getManualGpuFunc = vulkanStartupSettings.GetManualGpuFunc;
		rateGpuSuitability = vulkanStartupSettings.RateGpuSuitability;

		// layers/extensions
		HashSet<string> requiredEngineInstanceLayerProperties = new([
#if DEBUG
				"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
		]);

		HashSet<string> requiredEngineInstanceExtensionProperties = new([
				Vk.KhrSurfaceExtensionName, //
				Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
				Vk.ExtDebugUtilsExtensionName,
#endif
		]);

		if (game.IsGlfwEnabled) { requiredEngineInstanceExtensionProperties.UnionWith(GLFW.GetRequiredInstanceExtensions()); }

		HashSet<string> requiredEngineDeviceExtensionProperties = new([
				Vk.KhrSwapchainExtensionName, //
				Vk.KhrDynamicRenderingExtensionName,
		]);

		HashSet<string> requiredInstanceLayerProperties = new(requiredEngineInstanceLayerProperties);
		requiredInstanceLayerProperties.UnionWith(vulkanStartupSettings.RequiredInstanceLayerProperties);
		RequiredInstanceLayerProperties = requiredInstanceLayerProperties.ToArray();

		HashSet<string> requiredInstanceExtensionProperties = new(requiredEngineInstanceExtensionProperties);
		requiredInstanceExtensionProperties.UnionWith(vulkanStartupSettings.RequiredInstanceExtensionProperties);
		RequiredInstanceExtensionProperties = requiredInstanceExtensionProperties.ToArray();

		HashSet<string> requiredDeviceExtensionProperties = new(requiredEngineDeviceExtensionProperties);
		requiredDeviceExtensionProperties.UnionWith(vulkanStartupSettings.RequiredDeviceExtensionProperties);
		RequiredDeviceExtensionProperties = requiredDeviceExtensionProperties.ToArray();

		// check for instance/extension properties
		VkLayerProperties[] availableInstanceLayerProperties = GetAvailableInstanceLayerProperties();
		PrintInstanceLayerProperties(availableInstanceLayerProperties);
		VkExtensionProperties[] availableInstanceExtensionProperties = GetAvailableInstanceExtensionProperties();
		PrintInstanceExtensionProperties(availableInstanceExtensionProperties);

		CheckForInstanceLayerProperties(availableInstanceLayerProperties);
		CheckForInstanceExtensionProperties(availableInstanceExtensionProperties);

		// create instance
		VulkanInstance = new(game, vulkanStartupSettings, this);
		VKLoader.SetInstance(VulkanInstance.VkInstance); // set opentk instance
		Logger.Trace("Created VkInstance");

		// debugger
		debugMessenger = new(VulkanInstance, vulkanStartupSettings.EnabledDebugMessageSeverities, vulkanStartupSettings.EnabledDebugMessageTypes);
		Logger.Trace("Created Vulkan Debug Messenger");

		// get physical gpus
		physicalGpus = GetSuitablePhysicalGpus(VulkanInstance, vulkanStartupSettings);
		Logger.Trace($"Sorted {physicalGpus.Length} physical gpus");
		PrintPhysicalGpus();
	}

	internal void Cleanup() {
		Logger.Trace("Cleaning up final resources...");

		Logger.Trace("- Cleaning up debugger...");
		debugMessenger.Cleanup();

		Logger.Trace("- Cleaning up instance...");
		VulkanInstance.Cleanup();
	}

	[MustUseReturnValue]
	private static VkLayerProperties[] GetAvailableInstanceLayerProperties() {
		uint layerCount;
		Vk.EnumerateInstanceLayerProperties(&layerCount, null);

		if (layerCount == 0) { return Array.Empty<VkLayerProperties>(); }

		VkLayerProperties[] layerProperties = new VkLayerProperties[layerCount];
		fixed (VkLayerProperties* layerPropertiesPtr = layerProperties) {
			Vk.EnumerateInstanceLayerProperties(&layerCount, layerPropertiesPtr);
			return layerProperties;
		}
	}

	[MustUseReturnValue]
	private static VkExtensionProperties[] GetAvailableInstanceExtensionProperties() {
		uint extensionCount;
		Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, null);

		if (extensionCount == 0) { return Array.Empty<VkExtensionProperties>(); }

		VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
		fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
			Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, extensionPropertiesPtr);
			return extensionProperties;
		}
	}

	private void CheckForInstanceLayerProperties(VkLayerProperties[] availableInstanceLayerProperties) {
		if (availableInstanceLayerProperties.Length == 0) { throw new VulkanException("Could not find any instance layer properties"); }
		if (!CheckSupportForInstanceLayerProperties(availableInstanceLayerProperties, RequiredInstanceLayerProperties, out string[]? missingLayers)) {
			foreach (string missingLayer in missingLayers) { Logger.Fatal($"Layer \'{missingLayer}\' is not available"); } // TODO allow user to decide what to do for each missing
			throw new VulkanException("Requested validation layers are not available");
		}

		return;

		static bool CheckSupportForInstanceLayerProperties(VkLayerProperties[] layerProperties, string[] wantedLayers, [NotNullWhen(false)] out string[]? missing) {
			List<string> missingList = new();

			foreach (string wantedLayer in wantedLayers) {
				bool found = false;

				foreach (VkLayerProperties properties in layerProperties) {
					ReadOnlySpan<byte> layerName = properties.layerName;
					if (Encoding.UTF8.GetString(layerName[..layerName.IndexOf((byte)0)]) == wantedLayer) {
						found = true;
						break;
					}
				}

				if (!found) { missingList.Add(wantedLayer); }
			}

			missing = missingList.Count == 0 ? null : missingList.ToArray();
			return missing == null;
		}
	}

	private void CheckForInstanceExtensionProperties(VkExtensionProperties[] availableInstanceExtensionProperties) {
		if (availableInstanceExtensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }
		if (!CheckSupportForInstanceExtensionProperties(availableInstanceExtensionProperties, RequiredInstanceExtensionProperties, out string[]? missingExtensions)) {
			foreach (string missingExtension in missingExtensions) { Logger.Fatal($"Extension \'{missingExtension}\' is not available"); } // TODO allow user to decide what to do for each missing
			throw new VulkanException("Requested instance extensions are not available");
		}

		return;

		static bool CheckSupportForInstanceExtensionProperties(VkExtensionProperties[] extensionProperties, string[] wantedExtensions, [NotNullWhen(false)] out string[]? missing) {
			List<string> missingList = new();

			foreach (string wantedExtension in wantedExtensions) {
				bool found = false;

				foreach (VkExtensionProperties properties in extensionProperties) {
					ReadOnlySpan<byte> extensionName = properties.extensionName;
					if (Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension) {
						found = true;
						break;
					}
				}

				if (!found) { missingList.Add(wantedExtension); }
			}

			missing = missingList.Count == 0 ? null : missingList.ToArray();
			return missing == null;
		}
	}

	private static void PrintInstanceLayerProperties(VkLayerProperties[] layerProperties) {
		string[] arr = new string[layerProperties.Length];
		for (int i = 0; i < layerProperties.Length; i++) {
			ReadOnlySpan<byte> extensionName = layerProperties[i].layerName;
			arr[i] = Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]);
		}

		Logger.Trace($"The following instance layer properties are available: {arr.ElementsAsString()}");
	}

	private static void PrintInstanceExtensionProperties(VkExtensionProperties[] extensionProperties) {
		string[] arr = new string[extensionProperties.Length];
		for (int i = 0; i < extensionProperties.Length; i++) {
			ReadOnlySpan<byte> extensionName = extensionProperties[i].extensionName;
			arr[i] = Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]);
		}

		Logger.Trace($"The following instance extension properties are available: {arr.ElementsAsString()}");
	}

	[MustUseReturnValue]
	private PhysicalGpu[] GetSuitablePhysicalGpus(VulkanInstance vulkanInstance, VulkanStartupSettings vulkanStartupSettings) {
		VkPhysicalDevice[] availablePhysicalDevices = GetAvailablePhysicalDevices(vulkanInstance);

		List<PhysicalGpu> physicalGpus = new();
		foreach (VkPhysicalDevice physicalDevice in availablePhysicalDevices) {
			VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new();
			VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new();
			Vk.GetPhysicalDeviceProperties2(physicalDevice, &physicalDeviceProperties2);
			Vk.GetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

			PhysicalGpuProperties properties = new(physicalDeviceProperties2, physicalDeviceFeatures2);

			if (!IsPhysicalDeviceSuitableForEngine(vulkanStartupSettings, properties)) { continue; }
			if (!vulkanStartupSettings.IsPhysicalDeviceSuitable?.Invoke(properties) ?? false) { continue; }

			VkExtensionProperties[] physicalDeviceExtensionProperties = GetPhysicalDeviceExtensionProperties(physicalDevice);
			if (physicalDeviceExtensionProperties.Length == 0) { continue; }

			if (!CheckDeviceExtensionSupport(physicalDeviceExtensionProperties, RequiredDeviceExtensionProperties, out _)) { continue; } // TODO allow the user to handle missing?

			physicalGpus.Add(new(physicalDevice, properties));
		}

		return physicalGpus.ToArray();

		[MustUseReturnValue]
		static VkPhysicalDevice[] GetAvailablePhysicalDevices(VulkanInstance vulkanInstance) {
			uint deviceCount;
			Vk.EnumeratePhysicalDevices(vulkanInstance.VkInstance, &deviceCount, null);

			if (deviceCount == 0) { return Array.Empty<VkPhysicalDevice>(); }

			VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
			fixed (VkPhysicalDevice* physicalDevicesPtr = physicalDevices) {
				Vk.EnumeratePhysicalDevices(vulkanInstance.VkInstance, &deviceCount, physicalDevicesPtr);
				return physicalDevices;
			}
		}

		[MustUseReturnValue]
		static bool IsPhysicalDeviceSuitableForEngine(VulkanStartupSettings vulkanStartupSettings, PhysicalGpuProperties physicalGpuProperties) {
			VkPhysicalDeviceProperties properties = physicalGpuProperties.PhysicalDeviceProperties2.properties;
			VkPhysicalDeviceFeatures features = physicalGpuProperties.PhysicalDeviceFeatures2.features;

			bool isValid = properties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu //
					or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu
					or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu;

			if (vulkanStartupSettings.AllowEnableAnisotropy) { isValid &= features.samplerAnisotropy == Vk.True; }

			return isValid;
		}

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
		static bool CheckDeviceExtensionSupport(VkExtensionProperties[] extensionProperties, string[] wantedExtensions, [NotNullWhen(false)] out string[]? missing) {
			List<string> missingList = new();

			foreach (string wantedExtension in wantedExtensions) {
				bool found = false;

				foreach (VkExtensionProperties properties in extensionProperties) {
					ReadOnlySpan<byte> extensionName = properties.extensionName;
					if (Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension) {
						found = true;
						break;
					}
				}

				if (!found) { missingList.Add(wantedExtension); }
			}

			missing = missingList.Count == 0 ? null : missingList.ToArray();
			return missingList.Count == 0;
		}
	}

	private void PrintPhysicalGpus() {
		foreach (PhysicalGpu physicalGpu in physicalGpus) {
			// TODO print gpus. support more verbose & simplified printing
		}
	}

	internal SurfaceReadyPhysicalGpu[] GetCapableGpus(Surface surface) {
		List<SurfaceReadyPhysicalGpu> boundPhysicalGpus = new();
		foreach (PhysicalGpu physicalGpu in physicalGpus) {
			VkPhysicalDevice physicalDevice = physicalGpu.VkPhysicalDevice;

			if (!FindQueueFamilies(physicalDevice, surface.VkSurface, out QueueFamilyIndices? queueFamilyIndices)) { continue; }

			// any other checks?
			// TODO how do i properly query for support? i don't think i was doing it correctly. see https://github.com/MrUnknown404/Engine/blob/main/Src/Client/Graphics/Vulkan/SwapChain.cs#L72

			boundPhysicalGpus.Add(new(physicalGpu, surface, queueFamilyIndices.Value));
		}

		return boundPhysicalGpus.ToArray();

		[MustUseReturnValue]
		static VkQueueFamilyProperties2[] GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice) {
			uint queueFamilyPropertyCount = 0;
			Vk.GetPhysicalDeviceQueueFamilyProperties2(physicalDevice, &queueFamilyPropertyCount, null);

			if (queueFamilyPropertyCount == 0) { return Array.Empty<VkQueueFamilyProperties2>(); }

			VkQueueFamilyProperties2[] queueFamilyProperties2 = new VkQueueFamilyProperties2[queueFamilyPropertyCount];
			Array.Fill(queueFamilyProperties2, new() { sType = VkStructureType.StructureTypeQueueFamilyProperties2, });

			fixed (VkQueueFamilyProperties2* queueFamilyPropertiesPtr = queueFamilyProperties2) {
				Vk.GetPhysicalDeviceQueueFamilyProperties2(physicalDevice, &queueFamilyPropertyCount, queueFamilyPropertiesPtr);
				return queueFamilyProperties2;
			}
		}

		[MustUseReturnValue]
		static bool FindQueueFamilies(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, [NotNullWhen(true)] out QueueFamilyIndices? queueFamilyIndices) {
			uint? graphicsFamily = null;
			uint? presentFamily = null;
			uint? transferFamily = null;

			VkQueueFamilyProperties2[] queueFamilyProperties2 = GetPhysicalDeviceQueueFamilyProperties(physicalDevice);

			for (uint i = 0; i < queueFamilyProperties2.Length; i++) {
				VkQueueFamilyProperties queueFamilyProperties = queueFamilyProperties2[i].queueFamilyProperties;
				if ((queueFamilyProperties.queueFlags & VkQueueFlagBits.QueueGraphicsBit) != 0) { graphicsFamily = i; }
				if ((queueFamilyProperties.queueFlags & VkQueueFlagBits.QueueTransferBit) != 0) { transferFamily = i; }

				int presentSupport;
				Vk.GetPhysicalDeviceSurfaceSupportKHR(physicalDevice, i, surface, &presentSupport);
				if (presentSupport == Vk.True) { presentFamily = i; }

				if (graphicsFamily != null && presentFamily != null && transferFamily != null) { // if filled. we're done
					queueFamilyIndices = new(graphicsFamily.Value, transferFamily.Value, presentFamily.Value);
					return true;
				}
			}

			queueFamilyIndices = null;
			return false;
		}
	}

	internal SurfaceReadyPhysicalGpu? SelectGpu(SurfaceReadyPhysicalGpu[] capableGpus) {
		switch (selectGpuMode) {
			case SelectGpuMode.Manual: return getManualGpuFunc?.Invoke(capableGpus);
			case SelectGpuMode.HighestRated:
				if (rateGpuSuitability == null) { return null; }

				SurfaceReadyPhysicalGpu? bestDevice = null;
				int bestDeviceScore = int.MinValue;

				foreach (SurfaceReadyPhysicalGpu device in capableGpus) {
					int score = rateGpuSuitability(device);
					if (score > bestDeviceScore) {
						bestDevice = device;
						bestDeviceScore = score;
					}
				}

				return bestDevice;
			default: throw new InvalidEnumArgumentException();
		}
	}
}