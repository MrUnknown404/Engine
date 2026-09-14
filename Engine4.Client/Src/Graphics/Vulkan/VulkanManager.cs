using System.Diagnostics.CodeAnalysis;
using System.Text;
using Engine4.Client.Utility;
using Engine4.Client.Utility.Exceptions;
using Engine4.IO;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Utils;

namespace Engine4.Client.Graphics.Vulkan;

public sealed unsafe class VulkanManager {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	public static readonly string[] RequiredEngineInstanceLayerProperties = [
#if DEBUG
			"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
	];

	public static readonly string[] RequiredEngineInstanceExtensionProperties = [
			Vk.KhrSurfaceExtensionName, //
			Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
			Vk.ExtDebugUtilsExtensionName,
#endif
	];

	public static readonly string[] RequiredEngineDeviceExtensionProperties = [
			Vk.KhrSwapchainExtensionName, //
			Vk.KhrDynamicRenderingExtensionName,
	];

	public VulkanResourceManager ResourceManager { get; }

	private readonly VulkanInstance vulkanInstance;
	private readonly VulkanDebugMessenger debugMessenger; // TODO remove in release builds
	private readonly PhysicalGpu[] physicalGpus;

	internal VulkanManager(GameClient game, VulkanStartupSettings vulkanSettings) {
		// get vulkan api version
		uint apiVersion;
		Vk.EnumerateInstanceVersion(&apiVersion);
		Logger.Debug($"- Version: {apiVersion} ({Vk.API_VERSION_MAJOR(apiVersion)}.{Vk.API_VERSION_MINOR(apiVersion)}.{Vk.API_VERSION_PATCH(apiVersion)})");

		// check for instance/extension properties
		VkLayerProperties[] availableInstanceLayerProperties = CheckForInstanceLayerProperties(vulkanSettings);
		VkExtensionProperties[] availableInstanceExtensionProperties = CheckForInstanceExtensionProperties(vulkanSettings);
		PrintInstanceLayerProperties(availableInstanceLayerProperties);
		PrintInstanceExtensionProperties(availableInstanceExtensionProperties);

		// create instance
		vulkanInstance = new(game, vulkanSettings);
		VKLoader.SetInstance(vulkanInstance.VkInstance);
		Logger.Trace("Created VkInstance");

		// debugger
		debugMessenger = new(vulkanInstance, vulkanSettings.EnabledDebugMessageSeverities, vulkanSettings.EnabledDebugMessageTypes);
		Logger.Trace("Created Vulkan Debug Messenger");

		physicalGpus = GetPhysicalGpus(vulkanInstance, vulkanSettings);
		Logger.Trace($"Sorted {physicalGpus.Length} physical gpus");

		PrintPhysicalGpus();

		ResourceManager = new(vulkanInstance);
	}

	public void Cleanup() {
		Logger.Trace("- Cleaning up resources...");
		ResourceManager.Cleanup();

		debugMessenger.Cleanup();
		vulkanInstance.Cleanup();
	}

	[MustUseReturnValue]
	public static string[] GetRequiredInstanceLayerProperties(VulkanStartupSettings vulkanSettings) {
		HashSet<string> all = new(RequiredEngineInstanceLayerProperties);
		all.UnionWith(vulkanSettings.RequiredInstanceLayerProperties);
		return all.ToArray();
	}

	[MustUseReturnValue]
	public static string[] GetRequiredInstanceExtensionProperties(VulkanStartupSettings vulkanSettings) {
		HashSet<string> all = new(RequiredEngineInstanceExtensionProperties);
		all.UnionWith(vulkanSettings.RequiredInstanceExtensionProperties);
		return all.ToArray();
	}

	[MustUseReturnValue]
	public static string[] GetRequiredDeviceExtensionProperties(VulkanStartupSettings vulkanSettings) {
		HashSet<string> all = new(RequiredEngineDeviceExtensionProperties);
		all.UnionWith(vulkanSettings.RequiredDeviceExtensionProperties);
		return all.ToArray();
	}

	[MustUseReturnValue]
	private static VkLayerProperties[] CheckForInstanceLayerProperties(VulkanStartupSettings vulkanSettings) {
		string[] requiredInstanceLayerProperties = GetRequiredInstanceLayerProperties(vulkanSettings);
		VkLayerProperties[] availableInstanceLayerProperties = GetAvailableInstanceLayerProperties();

		if (availableInstanceLayerProperties.Length == 0) { throw new VulkanException("Could not find any instance layer properties"); }
		if (!CheckSupportForInstanceLayerProperties(availableInstanceLayerProperties, requiredInstanceLayerProperties, out string[]? missingLayers)) {
			foreach (string missingLayer in missingLayers) { Logger.Warn($"Layer \'{missingLayer}\' is not available"); } // TODO allow user to decide what to do for each missing
			throw new VulkanException("Requested validation layers are not available");
		}

		return availableInstanceLayerProperties;

		static VkLayerProperties[] GetAvailableInstanceLayerProperties() {
			uint layerCount;
			Vk.EnumerateInstanceLayerProperties(&layerCount, null);

			if (layerCount == 0) { return Array.Empty<VkLayerProperties>(); }

			VkLayerProperties[] layerProperties = new VkLayerProperties[layerCount];
			fixed (VkLayerProperties* layerPropertiesPtr = layerProperties) {
				Vk.EnumerateInstanceLayerProperties(&layerCount, layerPropertiesPtr);
				return layerProperties;
			}
		}

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

	[MustUseReturnValue]
	private static VkExtensionProperties[] CheckForInstanceExtensionProperties(VulkanStartupSettings vulkanSettings) {
		string[] requiredInstanceExtensionProperties = GetRequiredInstanceExtensionProperties(vulkanSettings);
		VkExtensionProperties[] availableInstanceExtensionProperties = GetAvailableInstanceExtensionProperties();

		if (availableInstanceExtensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }
		if (!CheckSupportForInstanceExtensionProperties(availableInstanceExtensionProperties, requiredInstanceExtensionProperties, out string[]? missingExtensions)) {
			foreach (string missingExtension in missingExtensions) { Logger.Warn($"Extension \'{missingExtension}\' is not available"); } // TODO allow user to decide what to do for each missing
			throw new VulkanException("Requested instance extensions are not available");
		}

		return availableInstanceExtensionProperties;

		static VkExtensionProperties[] GetAvailableInstanceExtensionProperties() {
			uint extensionCount;
			Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, null);

			if (extensionCount == 0) { return Array.Empty<VkExtensionProperties>(); }

			VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
			fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
				Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, extensionPropertiesPtr);
				return extensionProperties;
			}
		}

		static bool CheckSupportForInstanceExtensionProperties(VkExtensionProperties[] extensionProperties, string[] wantedExtensions, [NotNullWhen(false)] out string[]? missing) {
			List<string> missingList = new();

			foreach (string wantedExtension in wantedExtensions) {
				bool found = false;

				foreach (VkExtensionProperties properties in extensionProperties) {
					ReadOnlySpan<byte> extensionName = properties.extensionName; // TODO this has a desc. what is it?
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

		Logger.Trace($"- The following instance layer properties are available: {arr.ElementsAsString()}");
	}

	private static void PrintInstanceExtensionProperties(VkExtensionProperties[] extensionProperties) {
		string[] arr = new string[extensionProperties.Length];
		for (int i = 0; i < extensionProperties.Length; i++) {
			ReadOnlySpan<byte> extensionName = extensionProperties[i].extensionName;
			arr[i] = Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]);
		}

		Logger.Trace($"- The following instance extension properties are available: {arr.ElementsAsString()}");
	}

	[MustUseReturnValue]
	private static PhysicalGpu[] GetPhysicalGpus(VulkanInstance vulkanInstance, VulkanStartupSettings vulkanSettings) {
		string[] requiredDeviceExtensionProperties = GetRequiredDeviceExtensionProperties(vulkanSettings);
		VkPhysicalDevice[] availablePhysicalDevices = GetAvailablePhysicalDevices(vulkanInstance);

		List<PhysicalGpu> physicalGpus = new();
		foreach (VkPhysicalDevice physicalDevice in availablePhysicalDevices) {
			VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new();
			VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new();
			Vk.GetPhysicalDeviceProperties2(physicalDevice, &physicalDeviceProperties2);
			Vk.GetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

			PhysicalGpuProperties properties = new(physicalDeviceProperties2, physicalDeviceFeatures2);

			if (!IsPhysicalDeviceSuitableForEngine(vulkanSettings, properties)) { continue; }
			if (!vulkanSettings.IsPhysicalDeviceSuitable?.Invoke(properties) ?? false) { continue; }

			VkExtensionProperties[] physicalDeviceExtensionProperties = GetPhysicalDeviceExtensionProperties(physicalDevice);
			if (physicalDeviceExtensionProperties.Length == 0) { continue; }

			if (!CheckDeviceExtensionSupport(physicalDeviceExtensionProperties, requiredDeviceExtensionProperties, out _)) { continue; } // TODO allow the user to handle missing?

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
		static bool IsPhysicalDeviceSuitableForEngine(VulkanStartupSettings vulkanSettings, PhysicalGpuProperties physicalGpuProperties) {
			VkPhysicalDeviceProperties properties = physicalGpuProperties.PhysicalDeviceProperties2.properties;
			VkPhysicalDeviceFeatures features = physicalGpuProperties.PhysicalDeviceFeatures2.features;

			bool isValid = properties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu //
					or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu
					or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu;

			if (vulkanSettings.AllowEnableAnisotropy) { isValid &= features.samplerAnisotropy == Vk.True; }

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
		// TODO
	}
}