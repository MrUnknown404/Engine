using Engine4.Client.Utility;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan;

public sealed unsafe class VulkanInstance {
	public VkInstance VkInstance { get; } // TODO private

	internal VulkanInstance(GameClient game, VulkanStartupSettings vulkanSettings) {
		using StringUtf8Ptr appNamePtr = new(game.Name);
		using StringUtf8Ptr engineNamePtr = new(Engine4.Name);

		VkApplicationInfo applicationInfo = new() {
				pApplicationName = appNamePtr, //
				applicationVersion = game.Version.Packed,
				pEngineName = engineNamePtr,
				engineVersion = Engine4.Version.Packed,
				apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
		};

		string[] requiredInstanceLayerProperties = VulkanManager.GetRequiredInstanceLayerProperties(vulkanSettings);
		string[] requiredInstanceExtensionProperties = VulkanManager.GetRequiredInstanceExtensionProperties(vulkanSettings);

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
		VkInstance = Vk.CreateInstance(&instanceCreateInfo, null, &vkInstance) != VkResult.Success ? throw new Exception() : vkInstance; // TODO exception
	}

	internal void Cleanup() => Vk.DestroyInstance(VkInstance, null);
}