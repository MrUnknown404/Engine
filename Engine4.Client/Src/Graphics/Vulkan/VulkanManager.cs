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
		VKLoader.SetInstance(vulkanInstance.VkInstance); // move?
		Logger.Trace("Created VkInstance");

		// debugger
		debugMessenger = new(vulkanInstance, vulkanSettings.EnabledDebugMessageSeverities, vulkanSettings.EnabledDebugMessageTypes);
		Logger.Debug("Created Vulkan Debug Messenger");

		// TODO get devices

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
		VkLayerProperties[] availableLayerProperties = GetAvailableInstanceLayerProperties();

		if (availableLayerProperties.Length == 0) { throw new VulkanException("Could not find any instance layer properties"); }
		if (!CheckSupportForInstanceLayerProperties(availableLayerProperties, requiredInstanceLayerProperties, out string[]? missingLayers)) {
			foreach (string missingLayer in missingLayers) { Logger.Warn($"Layer \'{missingLayer}\' is not available"); } // TODO allow user to decide what to do for each missing
			throw new VulkanException("Requested validation layers are not available");
		}

		return availableLayerProperties;

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
		VkExtensionProperties[] instanceExtensionProperties = GetAvailableInstanceExtensionProperties();

		if (instanceExtensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }
		if (!CheckSupportForInstanceExtensionProperties(instanceExtensionProperties, requiredInstanceExtensionProperties, out string[]? missingExtensions)) {
			foreach (string missingExtension in missingExtensions) { Logger.Warn($"Extension \'{missingExtension}\' is not available"); } // TODO allow user to decide what to do for each missing
			throw new VulkanException("Requested instance extensions are not available");
		}

		return instanceExtensionProperties;

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
}