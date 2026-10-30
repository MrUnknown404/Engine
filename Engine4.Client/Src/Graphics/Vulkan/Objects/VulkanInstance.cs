using Engine4.Client.Utility;
using Engine4.Utility.Versions;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class VulkanInstance : VulkanResource {
	private readonly VkInstance vkInstance;
	public VkInstance VkInstance => vkInstance;
	public override ulong Handle => (ulong)VkInstance.Handle;

	internal VulkanInstance(string appName, IPackableVersion appVersion, SetupVulkanSettings setupVulkanSettings, VulkanSettings vulkanSettings) {
		using StringUtf8Ptr appNamePtr = new(appName);
		using StringUtf8Ptr engineNamePtr = new(Engine4.Name);

		VkApplicationInfo applicationInfo = new() {
				pApplicationName = appNamePtr, //
				applicationVersion = appVersion.Packed,
				pEngineName = engineNamePtr,
				engineVersion = Engine4.Version.Packed,
				apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
		};

		using StringArrayUtf8Ptr requiredInstanceLayerPropertiesPtr = new(vulkanSettings.RequiredInstanceLayerProperties);
		using StringArrayUtf8Ptr requiredInstanceExtensionPropertiesPtr = new(vulkanSettings.RequiredInstanceExtensionProperties);

#if DEBUG
		VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo =
				VulkanDebugMessenger.CreateDebugUtilsMessengerCreateInfoEXT(setupVulkanSettings.EnabledDebugMessageSeverities, setupVulkanSettings.EnabledDebugMessageTypes);
#endif

		VkInstanceCreateInfo instanceCreateInfo = new() {
				pApplicationInfo = &applicationInfo, //
#if DEBUG
				pNext = &messengerCreateInfo,
#endif
				ppEnabledLayerNames = requiredInstanceLayerPropertiesPtr,
				enabledLayerCount = (uint)vulkanSettings.RequiredInstanceLayerProperties.Length,
				ppEnabledExtensionNames = requiredInstanceExtensionPropertiesPtr,
				enabledExtensionCount = (uint)vulkanSettings.RequiredInstanceExtensionProperties.Length,
		};

		fixed (VkInstance* pVkInstance = &vkInstance) { VkH.ThrowIfNotSuccess(Vk.CreateInstance(&instanceCreateInfo, null, pVkInstance), "Failed to create instance"); }
	}

	protected internal override void Cleanup() => Vk.DestroyInstance(VkInstance, null);
}