using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Utility.Versions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Core.Native;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.Vulkan;

public unsafe class VulkanBackend : EngineGraphicsBackend {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public VulkanSettings Settings { get; init; } = new();

	public VkInstance? VkInstance { get; private set; }
	public PhysicalGpu[] PhysicalGpus { get; private set; } = Array.Empty<PhysicalGpu>();

#if DEBUG
	private static VkDebugUtilsMessengerEXT? vkDebugMessenger;
#endif

	public VulkanBackend(VulkanGraphicsApiHints graphicsApiHints) : base(GraphicsBackend.Vulkan, graphicsApiHints) { }

	protected internal override void Setup(GameClient gameClient) {
		Settings.Print();

		Logger.Debug("Loading Vulkan library...");
		VKLoader.Init();

		uint apiVersion;
		Vk.EnumerateInstanceVersion(&apiVersion);
		Logger.Debug($"- Version: {apiVersion} ({Vk.API_VERSION_MAJOR(apiVersion)}.{Vk.API_VERSION_MINOR(apiVersion)}.{Vk.API_VERSION_PATCH(apiVersion)})");

#if DEBUG
		{ // check for instance layer properties
			VkLayerProperties[] availableLayerProperties = EnumerateInstanceLayerProperties();
			if (availableLayerProperties.Length == 0) { throw new Engine3VulkanException("Could not find any instance layer properties"); }
			if (!CheckSupportForValidationLayers(availableLayerProperties, GetAllRequiredValidationLayers(), out string[]? missingExtensions)) {
				foreach (string missingExtension in missingExtensions) { Logger.Warn($"Layer \'{missingExtension}\' is not available"); } // TODO allow user to decide what to do for each missing
				throw new Engine3VulkanException("Requested validation layers are not available");
			}
		}
#endif

		{ // check for instance extension properties
			VkExtensionProperties[] instanceExtensionProperties = GetInstanceExtensionProperties();
			if (instanceExtensionProperties.Length == 0) { throw new Engine3VulkanException("Could not find any instance extension properties"); }
			if (!CheckSupportForInstanceExtensions(instanceExtensionProperties, GetAllRequiredInstanceExtensions(), out string[]? missingExtensions)) {
				foreach (string missingExtension in missingExtensions) { Logger.Warn($"Extension \'{missingExtension}\' is not available"); } // TODO allow user to decide what to do for each missing
				throw new Engine3VulkanException("Requested instance extensions are not available");
			}

			PrintInstanceExtensions(instanceExtensionProperties);
		}

		VkInstance = CreateVulkanInstance(gameClient.Name, gameClient.Version);
		VKLoader.SetInstance(VkInstance.Value);
		Logger.Info("Created Vulkan instance");

#if DEBUG
		vkDebugMessenger = CreateDebugMessenger(VkInstance.Value, Settings.EnabledDebugMessageSeverities, Settings.EnabledDebugMessageTypes);
		Logger.Debug("Created Vulkan Debug Messenger");
#endif

		PhysicalGpus = GetPhysicalGpus(VkInstance.Value, IsPhysicalDeviceSuitable, GetAllRequiredDeviceExtensions());
		Logger.Debug("Created Physical Gpus");
		PrintPhysicalGpus(Engine3.Debug);
	}

	public string[] GetAllRequiredValidationLayers() {
		HashSet<string> allValidationLayers = new();
		allValidationLayers.UnionWith(Engine3.RequiredValidationLayers);
		allValidationLayers.UnionWith(Settings.RequiredValidationLayers);
		return allValidationLayers.ToArray();
	}

	public string[] GetAllRequiredInstanceExtensions() {
		HashSet<string> allInstanceExtensions = new();
		allInstanceExtensions.UnionWith(Toolkit.Vulkan.GetRequiredInstanceExtensions().ToArray()); // no span support??
		allInstanceExtensions.UnionWith(Engine3.RequiredInstanceExtensions);
		allInstanceExtensions.UnionWith(Settings.RequiredInstanceExtensions);
		return allInstanceExtensions.ToArray();
	}

	public string[] GetAllRequiredDeviceExtensions() {
		HashSet<string> allDeviceExtensions = new();
		allDeviceExtensions.UnionWith(Engine3.RequiredDeviceExtensions);
		allDeviceExtensions.UnionWith(Settings.RequiredDeviceExtensions);
		return allDeviceExtensions.ToArray();
	}

	protected virtual bool IsPhysicalDeviceSuitable(VkPhysicalDeviceProperties physicalDeviceProperties, VkPhysicalDeviceFeatures physicalDeviceFeatures) {
		bool isValid = physicalDeviceProperties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu;

		if (Settings.AllowEnableAnisotropy) { isValid &= physicalDeviceFeatures.samplerAnisotropy == Vk.True; }

		return isValid;
	}

	protected internal virtual int RateGpuSuitability(PhysicalGpu physicalGpu) {
		VkPhysicalDeviceProperties deviceProperties = physicalGpu.PhysicalDeviceProperties2.properties;
		int score = 0;

		if (deviceProperties.deviceType == VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu) { score += 1000; }
		score += (int)deviceProperties.limits.maxImageDimension2D;

		return score;
	}

	protected internal override void Cleanup() {
		if (VkInstance is not { } vkInstance) { return; }

#if DEBUG
		if (vkDebugMessenger is { } debugMessage) {
			Vk.DestroyDebugUtilsMessengerEXT(vkInstance, debugMessage, null);
			vkDebugMessenger = null;
		}
#endif

		Vk.DestroyInstance(vkInstance, null);
		VkInstance = null;
	}

	private void PrintPhysicalGpus(bool verbose) {
		Logger.Debug("Found the following suitable gpus:");

		foreach (PhysicalGpu physicalGpu in PhysicalGpus) {
			if (verbose) {
				foreach (string line in physicalGpu.GetVerboseDescription()) { Logger.Trace(line); }
			} else { Logger.Debug(physicalGpu.GetSimpleDescription()); }
		}
	}

	[MustUseReturnValue]
	private VkInstance CreateVulkanInstance(string name, IPackableVersion version) {
		string[] enabledExtensions = GetAllRequiredInstanceExtensions();

		VkApplicationInfo applicationInfo = new() {
				pApplicationName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(name))),
				applicationVersion = version.Packed,
				pEngineName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(Engine3.Name))),
				engineVersion = Engine3.Version.Packed,
				apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
		};

		IntPtr requiredExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(enabledExtensions);

#if DEBUG
		string[] enabledLayers = GetAllRequiredValidationLayers();
		IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(enabledLayers);
		VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(Settings.EnabledDebugMessageSeverities, Settings.EnabledDebugMessageTypes);
#endif

		VkInstanceCreateInfo instanceCreateInfo = new() {
				pApplicationInfo = &applicationInfo,
#if DEBUG
				pNext = &messengerCreateInfo,
				enabledLayerCount = (uint)enabledLayers.Length,
				ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#endif
				enabledExtensionCount = (uint)enabledExtensions.Length,
				ppEnabledExtensionNames = (byte**)requiredExtensionsPtr,
		};

		VkInstance vkInstance;
		VkResult result = Vk.CreateInstance(&instanceCreateInfo, null, &vkInstance);

		MarshalTk.FreeStringArrayCoTaskMem(requiredExtensionsPtr, enabledExtensions.Length);
#if DEBUG
		MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, enabledLayers.Length);
#endif

		VkH.CheckIfSuccess(result, VulkanException.Reason.CreateInstance);
		return vkInstance;
	}

	[MustUseReturnValue]
	private static VkExtensionProperties[] GetInstanceExtensionProperties() {
		uint extensionCount;
		Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, null);

		if (extensionCount == 0) { return Array.Empty<VkExtensionProperties>(); }

		VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
		fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
			Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, extensionPropertiesPtr);
			return extensionProperties;
		}
	}

	[MustUseReturnValue]
	private static bool CheckSupportForInstanceExtensions(VkExtensionProperties[] instanceProperties, string[] wantedExtensions, [NotNullWhen(false)] out string[]? missingExtensions) {
		List<string> missing = new();

		foreach (string wantedExtension in wantedExtensions) {
			bool found = false;

			foreach (VkExtensionProperties properties in instanceProperties) {
				ReadOnlySpan<byte> extensionName = properties.extensionName;
				if (Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension) {
					found = true;
					break;
				}
			}

			if (!found) { missing.Add(wantedExtension); }
		}

		missingExtensions = missing.Count == 0 ? null : missing.ToArray();
		return missing.Count == 0;
	}

	[MustUseReturnValue]
	private static PhysicalGpu[] GetPhysicalGpus(VkInstance vkInstance, IsPhysicalDeviceSuitableDelegate isPhysicalDeviceSuitable, string[] requiredDeviceExtensions) {
		uint deviceCount;
		Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, null);

		if (deviceCount == 0) { return Array.Empty<PhysicalGpu>(); }

		VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
		fixed (VkPhysicalDevice* physicalDevicesPtr = physicalDevices) { Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, physicalDevicesPtr); }

		List<PhysicalGpu> physicalGpus = new();
		foreach (VkPhysicalDevice physicalDevice in physicalDevices) {
			VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new();
			VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new();
			Vk.GetPhysicalDeviceProperties2(physicalDevice, &physicalDeviceProperties2);
			Vk.GetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

			if (!isPhysicalDeviceSuitable(physicalDeviceProperties2.properties, physicalDeviceFeatures2.features)) { continue; }

			VkExtensionProperties[] physicalDeviceExtensionProperties = GetPhysicalDeviceExtensionProperties(physicalDevice);
			if (physicalDeviceExtensionProperties.Length == 0) { continue; }
			if (!CheckDeviceExtensionSupport(physicalDeviceExtensionProperties, requiredDeviceExtensions)) { continue; }

			physicalGpus.Add(new(physicalDevice, physicalDeviceProperties2, physicalDeviceFeatures2, physicalDeviceExtensionProperties));
		}

		return physicalGpus.ToArray();

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
		static bool CheckDeviceExtensionSupport(VkExtensionProperties[] physicalDeviceExtensionProperties, string[] wantedExtensions) =>
				wantedExtensions.All(wantedExtension => physicalDeviceExtensionProperties.Any(extensionProperties => {
					ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
					return Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension;
				}));
	}

	private static void PrintInstanceExtensions(VkExtensionProperties[] instanceExtensionProperties) {
		Logger.Trace("The following instance extensions are available:");
		foreach (VkExtensionProperties extensionProperties in instanceExtensionProperties) {
			ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
			Logger.Trace($"- {Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)])}");
		}
	}

#if DEBUG
	[MustUseReturnValue]
	private static VkDebugUtilsMessengerCreateInfoEXT CreateDebugUtilsMessengerCreateInfoEXT(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType) =>
			new() { messageSeverity = messageSeverity, messageType = messageType, pfnUserCallback = &DebugCallback, };

	[MustUseReturnValue]
	private static VkDebugUtilsMessengerEXT CreateDebugMessenger(VkInstance vkInstance, VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType) {
		VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(messageSeverity, messageType);
		VkDebugUtilsMessengerEXT debugMessenger;
		VkH.CheckIfSuccess(Vk.CreateDebugUtilsMessengerEXT(vkInstance, &messengerCreateInfo, null, &debugMessenger), VulkanException.Reason.CreateDebugMessenger);
		return debugMessenger;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl), })]
	private static int DebugCallback(VkDebugUtilsMessageSeverityFlagBitsEXT messageSeverity, VkDebugUtilsMessageTypeFlagBitsEXT messageType, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData) {
		string message = $"[Vulkan Callback] [{messageType switch {
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeDeviceAddressBindingBitExt => "Device Address Binding",
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt => "General",
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt => "Performance",
				VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt => "Validation",
				_ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null),
		}}] - {Marshal.PtrToStringAnsi((IntPtr)pCallbackData->pMessage) ?? throw new Exception()}";

		switch (messageSeverity) {
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt: Logger.Error(message); break;
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt: Logger.Warn(message); break;
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt: Logger.Info(message); break;
			case >= VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt: Logger.Debug(message); break;
			default: Logger.Warn($"Got unknown severity. {message}"); break;
		}

		return 0;
	}

	[MustUseReturnValue]
	private static VkLayerProperties[] EnumerateInstanceLayerProperties() {
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
	private static bool CheckSupportForValidationLayers(VkLayerProperties[] layerProperties, string[] wantedLayers, [NotNullWhen(false)] out string[]? missingExtensions) {
		List<string> missing = new();

		foreach (string wantedLayer in wantedLayers) {
			bool found = false;

			foreach (VkLayerProperties properties in layerProperties) {
				ReadOnlySpan<byte> layerName = properties.layerName;
				if (Encoding.UTF8.GetString(layerName[..layerName.IndexOf((byte)0)]) == wantedLayer) {
					found = true;
					break;
				}
			}

			if (!found) { missing.Add(wantedLayer); }
		}

		missingExtensions = missing.Count == 0 ? null : missing.ToArray();
		return missing.Count == 0;
	}
#endif

	public delegate bool IsPhysicalDeviceSuitableDelegate(VkPhysicalDeviceProperties physicalDeviceProperties, VkPhysicalDeviceFeatures physicalDeviceFeatures);
	public delegate int RateGpuSuitabilityDelegate(PhysicalGpu physicalGpu);
}