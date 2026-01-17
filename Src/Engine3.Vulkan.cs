using System.Diagnostics.CodeAnalysis;
using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using Engine3.Utils;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;
using GraphicsApi = Engine3.Graphics.GraphicsApi;

namespace Engine3 {
	public static partial class Engine3 {
		internal static readonly string[] RequiredValidationLayers = [
#if DEBUG
				"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
		];

		internal static readonly string[] RequiredInstanceExtensions = [
				Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
				Vk.ExtDebugUtilsExtensionName,
#endif
		];

		internal static readonly string[] RequiredDeviceExtensions = [ Vk.KhrSwapchainExtensionName, Vk.KhrDynamicRenderingExtensionName, ];

		public static VkInstance? VkInstance { get; private set; }
		public static VkPhysicalDevice[] VkPhysicalDevices { get; private set; } = Array.Empty<VkPhysicalDevice>();

		public static bool WasVulkanSetup { get; private set; }

#if DEBUG
		private static VkDebugUtilsMessengerEXT? vkDebugMessenger;
#endif

		private static void SetupVulkan(GameClient gameClient, Version4 engineVersion) {
#if DEBUG
			if (!VkH.CheckSupportForRequiredValidationLayers(gameClient.VkGetRequiredValidationLayers())) { throw new VulkanException("Requested validation layers are not available"); }
#endif

			VkExtensionProperties[] instanceExtensionProperties = VkH.GetInstanceExtensionProperties();
			if (instanceExtensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }

			string[] instanceExtensions = gameClient.VkGetInstanceExtensions();
			if (!VkH.CheckSupportForRequiredInstanceExtensions(instanceExtensionProperties, instanceExtensions)) { throw new VulkanException("Requested instance extensions are not available"); }

#if DEBUG
			VkH.PrintInstanceExtensions(instanceExtensionProperties);
#endif

			VKLoader.Init();

			VkInstance = VkH.CreateVulkanInstance(gameClient, Name, gameClient.Version, engineVersion);
			VKLoader.SetInstance(VkInstance.Value);
			Logger.Info("Created Vulkan instance");

#if DEBUG
			vkDebugMessenger = VkH.CreateDebugMessenger(VkInstance.Value, gameClient);
			Logger.Debug("Created Vulkan Debug Messenger");
#endif

			VkPhysicalDevices = VkH.GetPhysicalDevices(VkInstance.Value);
			Logger.Debug("Created Physical Devices");

			WasVulkanSetup = true;
		}

		private static unsafe void CleanupVulkan() {
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

		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		public class VulkanSettings : StartupSettings {
			public required ToolkitOptions ToolkitOptions { get; init; }
			public required VulkanGraphicsApiHints GraphicsApiHints { get; init; }

			public string[] RequiredValidationLayers { get; init; } = Array.Empty<string>();
			public string[] RequiredInstanceExtensions { get; init; } = Array.Empty<string>();
			public string[] RequiredDeviceExtensions { get; init; } = Array.Empty<string>();

			public VkDebugUtilsMessageSeverityFlagBitsEXT EnabledDebugMessageSeverities { get; init; } = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt |
																										 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt |
																										 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt |
																										 VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt;

			public VkDebugUtilsMessageTypeFlagBitsEXT EnabledDebugMessageTypes { get; init; } = VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt |
																								VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt |
																								VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt |
																								VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeDeviceAddressBindingBitExt;

			public byte MaxFramesInFlight { get; init; } = 2;

			public override GraphicsApi GraphicsApi => GraphicsApi.Vulkan;

			[SetsRequiredMembers]
			public VulkanSettings(string gameName, ToolkitOptions toolkitOptions, VulkanGraphicsApiHints? graphicsApiHints = null) : base(gameName) {
				ToolkitOptions = toolkitOptions;
				GraphicsApiHints = graphicsApiHints ?? new();

				ToolkitOptions.Logger = new TkLogger();
				ToolkitOptions.FeatureFlags = ToolkitFlags.EnableVulkan;
			}
		}
	}
}