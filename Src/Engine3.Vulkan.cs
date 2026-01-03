using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Graphics.Vulkan;
using Engine3.Utils;
using JetBrains.Annotations;
using OpenTK.Core.Native;
using OpenTK.Graphics;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;

namespace Engine3 {
	public static partial class Engine3 { // TODO all these preprocessor directives are getting annoying. i may want to refactor to take those in mind
		public static bool WasVulkanSetup { get; private set; }

		public static VkInstance? VkInstance { get; private set; }

#if DEBUG
		private static VkDebugUtilsMessengerEXT? vkDebugMessenger;
#endif

		private static void SetupVulkan(string vulkanAppTitle, Version4 gameVersion, Version4 engineVersion) {
#if DEBUG
			if (!CheckSupportForRequiredVulkanValidationLayers()) { throw new VulkanException("Requested validation layers are not available"); }
#endif
			if (!CheckSupportForRequiredVulkanExtensions()) { throw new VulkanException("Requested extensions are not available"); }

			VKLoader.Init();

			VkInstance = CreateVulkanInstance(vulkanAppTitle, gameVersion, engineVersion);
			VKLoader.SetInstance(VkInstance.Value);
			Logger.Info("Created Vulkan instance");

#if DEBUG
			vkDebugMessenger = VkH.CreateDebugMessenger(VkInstance.Value);
			Logger.Debug("Created Vulkan Debug Messenger");
#endif

			WasVulkanSetup = true;
		}

		[MustUseReturnValue]
		private static unsafe VkInstance CreateVulkanInstance(string title, Version4 gameVersion, Version4 engineVersion) {
			VkApplicationInfo vkApplicationInfo = new() {
					pApplicationName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(title))),
					applicationVersion = gameVersion.Packed,
					pEngineName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(Name))),
					engineVersion = engineVersion.Packed,
					apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
			};

			List<string> requiredExtensions = new();
			requiredExtensions.AddRange(Toolkit.Vulkan.GetRequiredInstanceExtensions());
			requiredExtensions.AddRange(VkH.RequiredInstanceExtensions);

			IntPtr requiredExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(requiredExtensions));
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(VkH.RequiredValidationLayers));

			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = VkH.CreateVkDebugUtilsMessengerCreateInfoEXT();
#endif

			VkInstanceCreateInfo vkCreateInfo = new() {
					pApplicationInfo = &vkApplicationInfo,
#if DEBUG
					pNext = &messengerCreateInfo,
					enabledLayerCount = (uint)VkH.RequiredValidationLayers.Count,
					ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#endif
					enabledExtensionCount = (uint)requiredExtensions.Count,
					ppEnabledExtensionNames = (byte**)requiredExtensionsPtr,
			};

			VkInstance vkInstance;
			if (Vk.CreateInstance(&vkCreateInfo, null, &vkInstance) != VkResult.Success) { throw new VulkanException("Failed to create Vulkan instance"); }

			MarshalTk.FreeStringArrayCoTaskMem(requiredExtensionsPtr, requiredExtensions.Count);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, VkH.RequiredValidationLayers.Count);
#endif

			return vkInstance;
		}

#if DEBUG
		private static bool CheckSupportForRequiredVulkanValidationLayers() {
			ReadOnlySpan<VkLayerProperties> availableLayerProperties = VkH.EnumerateInstanceLayerProperties();
			if (availableLayerProperties.Length == 0) { throw new VulkanException("Could not find any instance layer properties"); }

			foreach (string wantedLayer in VkH.RequiredValidationLayers) {
				bool layerFound = false;

				foreach (VkLayerProperties layerProperties in availableLayerProperties) {
					ReadOnlySpan<byte> layerName = layerProperties.layerName;
					if (Encoding.UTF8.GetString(layerName[..layerName.IndexOf((byte)0)]) == wantedLayer) {
						layerFound = true;
						break;
					}
				}

				if (!layerFound) { return false; }
			}

			return true;
		}
#endif

		private static bool CheckSupportForRequiredVulkanExtensions() {
			ReadOnlySpan<VkExtensionProperties> extensionProperties = VkH.EnumerateInstanceExtensionProperties();
			if (extensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }

			Logger.Debug("The following Vulkan extensions are available:");
			foreach (VkExtensionProperties extensionProperty in extensionProperties) {
				ReadOnlySpan<byte> extensionName = extensionProperty.extensionName;
				Logger.Debug($"- {Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)])}");
			}

			foreach (string wantedExtension in VkH.RequiredInstanceExtensions) {
				bool extensionFound = false;

				foreach (VkExtensionProperties extensionProperty in extensionProperties) {
					ReadOnlySpan<byte> extensionName = extensionProperty.extensionName;
					if (Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension) {
						extensionFound = true;
						break;
					}
				}

				if (!extensionFound) { return false; }
			}

			return true;
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

		public class VulkanSettings {
			public VulkanSettings() { }
		}
	}
}