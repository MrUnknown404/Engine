using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using Engine3.Utils;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;


namespace Engine3 {
	public static partial class Engine3 {
		public static bool WasVulkanSetup { get; private set; }

		public static VkInstance? VkInstance { get; private set; }
		public static VkPhysicalDevice[] VkPhysicalDevices { get; private set; } = Array.Empty<VkPhysicalDevice>();

#if DEBUG
		private static VkDebugUtilsMessengerEXT? vkDebugMessenger;
#endif

		private static void SetupVulkan(string appTitle, Version4 gameVersion, Version4 engineVersion) {
#if DEBUG
			if (!VkH.CheckSupportForRequiredValidationLayers()) { throw new VulkanException("Requested validation layers are not available"); }
#endif
			if (!VkH.CheckSupportForRequiredInstanceExtensions()) { throw new VulkanException("Requested instance extensions are not available"); }

			VKLoader.Init();

			VkInstance = VkH.CreateVulkanInstance(appTitle, Name, gameVersion, engineVersion);
			VKLoader.SetInstance(VkInstance.Value);
			Logger.Info("Created Vulkan instance");

#if DEBUG
			vkDebugMessenger = VkH.CreateDebugMessenger(VkInstance.Value);
			Logger.Debug("Created Vulkan Debug Messenger");
#endif

			VkPhysicalDevices = VkH.CreatePhysicalDevices(VkInstance.Value);
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
	}
}