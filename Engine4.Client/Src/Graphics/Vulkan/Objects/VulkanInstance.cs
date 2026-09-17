using Engine4.Client.Utility;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class VulkanInstance {
	internal VkInstance VkInstance { get; }

	internal VulkanInstance(GameClient game, VulkanStartupSettings vulkanSettings, VulkanManager vulkanManager) {
		using StringUtf8Ptr appNamePtr = new(game.Name);
		using StringUtf8Ptr engineNamePtr = new(Engine4.Name);

		VkApplicationInfo applicationInfo = new() {
				pApplicationName = appNamePtr, //
				applicationVersion = game.Version.Packed,
				pEngineName = engineNamePtr,
				engineVersion = Engine4.Version.Packed,
				apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
		};

		string[] requiredInstanceLayerProperties = vulkanManager.RequiredInstanceLayerProperties;
		string[] requiredInstanceExtensionProperties = vulkanManager.RequiredInstanceExtensionProperties;

		using StringArrayUtf8Ptr requiredInstanceLayerPropertiesPtr = new(requiredInstanceLayerProperties);
		using StringArrayUtf8Ptr requiredInstanceExtensionPropertiesPtr = new(requiredInstanceExtensionProperties);

#if DEBUG
		VkDebugUtilsMessengerCreateInfoEXT debugMessengerCreateInfo = VulkanDebugMessenger.CreateDebugUtilsMessengerCreateInfoEXT(vulkanSettings.EnabledDebugMessageSeverities, vulkanSettings.EnabledDebugMessageTypes);
#endif

		VkInstanceCreateInfo instanceCreateInfo = new() {
				pApplicationInfo = &applicationInfo, //
#if DEBUG
				pNext = &debugMessengerCreateInfo,
#endif
				ppEnabledLayerNames = requiredInstanceLayerPropertiesPtr,
				enabledLayerCount = (uint)requiredInstanceLayerProperties.Length,
				ppEnabledExtensionNames = requiredInstanceExtensionPropertiesPtr,
				enabledExtensionCount = (uint)requiredInstanceExtensionProperties.Length,
		};

		VkInstance vkInstance;
		VkH.CheckSuccess(Vk.CreateInstance(&instanceCreateInfo, null, &vkInstance), "Failed to create instance");
		VkInstance = vkInstance;
	}

	internal void Cleanup() => Vk.DestroyInstance(VkInstance, null);
}