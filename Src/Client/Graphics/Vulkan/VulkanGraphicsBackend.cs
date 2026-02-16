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
using USharpLibs.Common.Utils;

namespace Engine3.Client.Graphics.Vulkan {
	public unsafe class VulkanGraphicsBackend : EngineGraphicsBackend {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public string[] RequiredValidationLayers { get; init; } = Array.Empty<string>();
		public string[] RequiredInstanceExtensions { get; init; } = Array.Empty<string>();
		public string[] RequiredDeviceExtensions { get; init; } = Array.Empty<string>();

		public VkDebugUtilsMessageSeverityFlagBitsEXT EnabledDebugMessageSeverities { get; init; } = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt |
																									 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt |
																									 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt |
																									 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt;

		public VkDebugUtilsMessageTypeFlagBitsEXT EnabledDebugMessageTypes { get; init; } = VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt |
																							VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt |
																							VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt;

		public VkPresentModeKHR PresentMode { get; init; } = VkPresentModeKHR.PresentModeImmediateKhr;

		public byte MaxFramesInFlight {
			get;
			init {
				if (MaxFramesInFlight == 0) { throw new Engine3VulkanException($"{nameof(MaxFramesInFlight)} cannot be zero"); }
				field = value;
			}
		} = 2;

		public bool AllowEnableAnisotropy { get; init; } = true;

		public VkInstance? VkInstance { get; private set; }
		public PhysicalGpu[] PhysicalGpus { get; private set; } = Array.Empty<PhysicalGpu>();

#if DEBUG
		private static VkDebugUtilsMessengerEXT? vkDebugMessenger;
#endif

		public VulkanGraphicsBackend(VulkanGraphicsApiHints graphicsApiHints) : base(GraphicsBackend.Vulkan, graphicsApiHints) { }

		protected internal override void Setup(GameClient gameClient) {
			PrintSettings();

			Logger.Debug("Loading Vulkan library...");
			VKLoader.Init();

			uint apiVersion;
			Vk.EnumerateInstanceVersion(&apiVersion);
			Logger.Debug($"- Version: {apiVersion} ({Vk.API_VERSION_MAJOR(apiVersion)}.{Vk.API_VERSION_MINOR(apiVersion)}.{Vk.API_VERSION_PATCH(apiVersion)})");

#if DEBUG
			VkLayerProperties[] availableLayerProperties = EnumerateInstanceLayerProperties();
			if (availableLayerProperties.Length == 0) { throw new Engine3VulkanException("Could not find any instance layer properties"); }
			if (!CheckSupportForRequiredValidationLayers(availableLayerProperties, GetAllRequiredValidationLayers())) { throw new Engine3VulkanException("Requested validation layers are not available"); }
#endif

			VkExtensionProperties[] instanceExtensionProperties = GetInstanceExtensionProperties();
			if (instanceExtensionProperties.Length == 0) { throw new Engine3VulkanException("Could not find any instance extension properties"); }
			if (!CheckSupportForRequiredInstanceExtensions(instanceExtensionProperties, GetAllRequiredInstanceExtensions())) { throw new Engine3VulkanException("Requested instance extensions are not available"); }

			PrintInstanceExtensions(instanceExtensionProperties);

			VkInstance = CreateVulkanInstance(gameClient.Name, gameClient.Version);
			VKLoader.SetInstance(VkInstance.Value);
			Logger.Info("Created Vulkan instance");

#if DEBUG
			vkDebugMessenger = CreateDebugMessenger(VkInstance.Value, EnabledDebugMessageSeverities, EnabledDebugMessageTypes);
			Logger.Debug("Created Vulkan Debug Messenger");
#endif

			PhysicalGpus = GetPhysicalGpus(VkInstance.Value, IsPhysicalDeviceSuitable, RequiredDeviceExtensions);
			Logger.Debug("Created Physical Gpus");
			PrintPhysicalGpus(Engine3.Debug);
		}

		public string[] GetAllRequiredValidationLayers() {
			HashSet<string> allRequiredValidationLayers = new();
			allRequiredValidationLayers.UnionWith(Engine3.RequiredValidationLayers);
			allRequiredValidationLayers.UnionWith(RequiredValidationLayers);
			return allRequiredValidationLayers.ToArray();
		}

		public string[] GetAllRequiredInstanceExtensions() {
			HashSet<string> allInstanceExtensions = new();
			allInstanceExtensions.UnionWith(Toolkit.Vulkan.GetRequiredInstanceExtensions().ToArray()); // no span support??
			allInstanceExtensions.UnionWith(Engine3.RequiredInstanceExtensions);
			allInstanceExtensions.UnionWith(RequiredInstanceExtensions);
			return allInstanceExtensions.ToArray();
		}

		public string[] GetAllRequiredDeviceExtensions() {
			HashSet<string> allDeviceExtensions = new();
			allDeviceExtensions.UnionWith(Engine3.RequiredDeviceExtensions);
			allDeviceExtensions.UnionWith(RequiredDeviceExtensions);
			return allDeviceExtensions.ToArray();
		}

		[Obsolete]
		protected virtual bool IsPhysicalDeviceSuitable(VkPhysicalDeviceProperties physicalDeviceProperties, VkPhysicalDeviceFeatures physicalDeviceFeatures) { // TODO make parameter
			bool isValid = physicalDeviceProperties.deviceType is VkPhysicalDeviceType.PhysicalDeviceTypeIntegratedGpu or VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu or VkPhysicalDeviceType.PhysicalDeviceTypeVirtualGpu;

			if (AllowEnableAnisotropy) { isValid &= physicalDeviceFeatures.samplerAnisotropy == Vk.True; }

			return isValid;
		}

		[Obsolete]
		protected internal virtual int RateGpuSuitability(PhysicalGpu physicalGpu) { // TODO make parameter
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

		private void PrintSettings() {
			Logger.Trace("Vulkan Graphics Backend Settings");
			Logger.Trace($"- {nameof(RequiredValidationLayers)}: {RequiredValidationLayers.ElementsAsString()}");
			Logger.Trace($"- {nameof(RequiredInstanceExtensions)}: {RequiredInstanceExtensions.ElementsAsString()}");
			Logger.Trace($"- {nameof(RequiredDeviceExtensions)}: {RequiredDeviceExtensions.ElementsAsString()}");
			Logger.Trace($"- {nameof(EnabledDebugMessageSeverities)}: {EnabledDebugMessageSeverities}");
			Logger.Trace($"- {nameof(EnabledDebugMessageTypes)}: {EnabledDebugMessageTypes}");
			Logger.Trace($"- {nameof(PresentMode)}: {PresentMode}");
			Logger.Trace($"- {nameof(MaxFramesInFlight)}: {MaxFramesInFlight}");
			Logger.Trace($"- {nameof(AllowEnableAnisotropy)}: {AllowEnableAnisotropy}");
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
			string[] requiredInstanceExtensions = GetAllRequiredInstanceExtensions();
			string[] requiredValidationLayers = GetAllRequiredValidationLayers();

			VkApplicationInfo applicationInfo = new() {
					pApplicationName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(name))),
					applicationVersion = version.Packed,
					pEngineName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(Engine3.Name))),
					engineVersion = Engine3.Version.Packed,
					apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
			};

			IntPtr requiredExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredInstanceExtensions);
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredValidationLayers);

			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(EnabledDebugMessageSeverities, EnabledDebugMessageTypes);
#endif

			VkInstanceCreateInfo instanceCreateInfo = new() {
					pApplicationInfo = &applicationInfo,
#if DEBUG
					pNext = &messengerCreateInfo,
					enabledLayerCount = (uint)requiredValidationLayers.Length,
					ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#endif
					enabledExtensionCount = (uint)requiredInstanceExtensions.Length,
					ppEnabledExtensionNames = (byte**)requiredExtensionsPtr,
			};

			VkInstance vkInstance;
			VkResult result = Vk.CreateInstance(&instanceCreateInfo, null, &vkInstance);

			MarshalTk.FreeStringArrayCoTaskMem(requiredExtensionsPtr, requiredInstanceExtensions.Length);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
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
		private static bool CheckSupportForRequiredInstanceExtensions(VkExtensionProperties[] instanceExtensionProperties, string[] requiredInstanceExtensions) =>
				requiredInstanceExtensions.All(wantedExtension => instanceExtensionProperties.Any(extensionProperties => {
					ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
					return Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension;
				}));

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
			static bool CheckDeviceExtensionSupport(VkExtensionProperties[] physicalDeviceExtensionProperties, string[] requiredDeviceExtensions) =>
					requiredDeviceExtensions.All(wantedExtension => physicalDeviceExtensionProperties.Any(extensionProperties => {
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
		private static bool CheckSupportForRequiredValidationLayers(VkLayerProperties[] availableLayerProperties, string[] requiredValidationLayers) =>
				requiredValidationLayers.All(wantedLayer => availableLayerProperties.Any(layerProperties => {
					ReadOnlySpan<byte> layerName = layerProperties.layerName;
					return Encoding.UTF8.GetString(layerName[..layerName.IndexOf((byte)0)]) == wantedLayer;
				}));

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

#endif

		public delegate bool IsPhysicalDeviceSuitableDelegate(VkPhysicalDeviceProperties physicalDeviceProperties, VkPhysicalDeviceFeatures physicalDeviceFeatures);
		public delegate int RateGpuSuitabilityDelegate(PhysicalGpu physicalGpu);
	}
}