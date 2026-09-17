using System.Diagnostics.CodeAnalysis;
using System.Text;
using Engine4.Client.Graphics.Vulkan.Objects;
using Engine4.Client.Rendering;
using Engine4.Client.Utility;
using Engine4.Client.Utility.Exceptions;
using Engine4.Client.Utility.Extensions;
using Engine4.IO;
using Engine4.Utility.Math;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Utils;

namespace Engine4.Client.Graphics.Vulkan;

public sealed unsafe class VulkanManager {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	// engine
	public static readonly string[] RequiredEngineInstanceLayerProperties;
	public static readonly string[] RequiredEngineInstanceExtensionProperties;
	public static readonly string[] RequiredEngineDeviceExtensionProperties;

	static VulkanManager() {
		HashSet<string> requiredEngineInstanceLayerProperties = new([
#if DEBUG
				"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
		]);

		HashSet<string> requiredEngineInstanceExtensionProperties = new([
				Vk.KhrSurfaceExtensionName, Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
				Vk.ExtDebugUtilsExtensionName,
#endif
		]);

		HashSet<string> requiredEngineDeviceExtensionProperties = new([
				Vk.KhrSwapchainExtensionName, //
				Vk.KhrDynamicRenderingExtensionName,
		]);

		requiredEngineInstanceLayerProperties.UnionWith(Engine4.OperatingSystem.GetRequiredInstanceLayerProperties());
		requiredEngineInstanceExtensionProperties.UnionWith(Engine4.OperatingSystem.GetRequiredInstanceExtensionProperties());
		requiredEngineDeviceExtensionProperties.UnionWith(Engine4.OperatingSystem.GetRequiredDeviceExtensionProperties());

		RequiredEngineInstanceLayerProperties = requiredEngineInstanceLayerProperties.ToArray();
		RequiredEngineInstanceExtensionProperties = requiredEngineInstanceExtensionProperties.ToArray();
		RequiredEngineDeviceExtensionProperties = requiredEngineDeviceExtensionProperties.ToArray();
	}

	internal VulkanInstance VulkanInstance { get; } // TODO private

#if DEBUG
	private readonly VulkanDebugMessenger debugMessenger;
#endif

	public VkPresentModeKHR PresentMode { get; } // TODO support setting this at runtime
	public byte MaxFramesInFlight { get; }

	private readonly PhysicalGpu[] physicalGpus;

	private readonly List<RenderTarget> renderTargets = new(); // TODO allow removal

	public readonly string[] RequiredInstanceLayerProperties;
	public readonly string[] RequiredInstanceExtensionProperties;
	public readonly string[] RequiredDeviceExtensionProperties;

	private readonly SelectGpuMode selectGpuMode;
	private readonly SelectGpuDelegate? getManualGpuFunc;
	private readonly RateGpuSuitabilityDelegate? rateGpuSuitability;

	internal VulkanManager(GameClient game, VulkanStartupSettings vulkanSettings) {
		PresentMode = vulkanSettings.PresentMode;
		MaxFramesInFlight = vulkanSettings.MaxFramesInFlight;

		selectGpuMode = vulkanSettings.SelectGpuMode;
		getManualGpuFunc = vulkanSettings.GetManualGpuFunc;
		rateGpuSuitability = vulkanSettings.RateGpuSuitability;

		// layers/extensions
		HashSet<string> requiredInstanceLayerProperties = new(RequiredEngineInstanceLayerProperties);
		requiredInstanceLayerProperties.UnionWith(vulkanSettings.RequiredInstanceLayerProperties);
		RequiredInstanceLayerProperties = requiredInstanceLayerProperties.ToArray();

		HashSet<string> requiredInstanceExtensionProperties = new(RequiredEngineInstanceExtensionProperties);
		requiredInstanceExtensionProperties.UnionWith(vulkanSettings.RequiredInstanceExtensionProperties);
		RequiredInstanceExtensionProperties = requiredInstanceExtensionProperties.ToArray();

		HashSet<string> requiredDeviceExtensionProperties = new(RequiredEngineDeviceExtensionProperties);
		requiredDeviceExtensionProperties.UnionWith(vulkanSettings.RequiredDeviceExtensionProperties);
		RequiredDeviceExtensionProperties = requiredDeviceExtensionProperties.ToArray();

		// get vulkan api version
		uint apiVersion;
		Vk.EnumerateInstanceVersion(&apiVersion);
		Logger.Debug($"- Version: {apiVersion} ({Vk.API_VERSION_MAJOR(apiVersion)}.{Vk.API_VERSION_MINOR(apiVersion)}.{Vk.API_VERSION_PATCH(apiVersion)})");

		// check for instance/extension properties
		VkLayerProperties[] availableInstanceLayerProperties = CheckForInstanceLayerProperties();
		VkExtensionProperties[] availableInstanceExtensionProperties = CheckForInstanceExtensionProperties();
		PrintInstanceLayerProperties(availableInstanceLayerProperties);
		PrintInstanceExtensionProperties(availableInstanceExtensionProperties);

		// create instance
		VulkanInstance = new(game, vulkanSettings, this);
		VKLoader.SetInstance(VulkanInstance.VkInstance); // set opentk instance
		Logger.Trace("Created VkInstance");

		// debugger
		debugMessenger = new(VulkanInstance, vulkanSettings.EnabledDebugMessageSeverities, vulkanSettings.EnabledDebugMessageTypes);
		Logger.Trace("Created Vulkan Debug Messenger");

		physicalGpus = GetPhysicalGpus(VulkanInstance, vulkanSettings);
		Logger.Trace($"Sorted {physicalGpus.Length} physical gpus");

		PrintPhysicalGpus();
	}

	public WindowRenderTarget CreateWindowRenderTarget(Window window, Color3 clearColor) {
		WindowRenderTarget renderTarget = new(window, this, clearColor);
		renderTargets.Add(renderTarget);
		return renderTarget;
	}

	public TextureRenderTarget CreateTextureRenderTarget() => throw new NotImplementedException(); // TODO
	public ConsoleRenderTarget CreateConsoleRenderTarget() => throw new NotImplementedException(); // TODO

	internal void Cleanup() {
		Logger.Trace("- Cleaning up resources...");

		foreach (RenderTarget renderTarget in renderTargets) { renderTarget.Cleanup(); } // logical gpus

		debugMessenger.Cleanup();
		VulkanInstance.Cleanup();
	}

	[MustUseReturnValue]
	private VkLayerProperties[] CheckForInstanceLayerProperties() {
		VkLayerProperties[] availableInstanceLayerProperties = GetAvailableInstanceLayerProperties();

		if (availableInstanceLayerProperties.Length == 0) { throw new VulkanException("Could not find any instance layer properties"); }
		if (!CheckSupportForInstanceLayerProperties(availableInstanceLayerProperties, RequiredInstanceLayerProperties, out string[]? missingLayers)) {
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
	private VkExtensionProperties[] CheckForInstanceExtensionProperties() {
		VkExtensionProperties[] availableInstanceExtensionProperties = GetAvailableInstanceExtensionProperties();

		if (availableInstanceExtensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }
		if (!CheckSupportForInstanceExtensionProperties(availableInstanceExtensionProperties, RequiredInstanceExtensionProperties, out string[]? missingExtensions)) {
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
	private PhysicalGpu[] GetPhysicalGpus(VulkanInstance vulkanInstance, VulkanStartupSettings vulkanSettings) {
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

		[MustUseReturnValue] // TODO split this for the 2 physical gpu classes. make a version of QueueFamilyIndices that doesn't use present queue?
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
			default: throw new ArgumentOutOfRangeException();
		}
	}
}