using OpenTK.Graphics.Vulkan;

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
	}
}