using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Utils;
using JetBrains.Annotations;
using NLog;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics.Vulkan {
	public static unsafe partial class VkH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary> Note: This must be set before you set up Vulkan </summary>
		public static readonly List<string> RequiredValidationLayers = new();
		/// <summary> Note: This must be set before you set up Vulkan </summary>
		public static readonly List<string> RequiredInstanceExtensions = [ Vk.KhrGetSurfaceCapabilities2ExtensionName, ];
		/// <summary> Note: This must be set before you set up Vulkan </summary>
		public static readonly List<string> RequiredDeviceExtensions = [ Vk.KhrSwapchainExtensionName, ];

		public static VkDebugUtilsMessageSeverityFlagBitsEXT EnabledDebugMessageSeverities {
			get;
			set {
				if (Engine3.WasVulkanSetup) { Logger.Warn("Attempted to set EnabledDebugMessageSeverities too late"); }
				field = value;
			}
		} = VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityVerboseBitExt |
			VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityInfoBitExt |
			VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityWarningBitExt |
			VkDebugUtilsMessageSeverityFlagBitsEXT.DebugUtilsMessageSeverityErrorBitExt;

		public static VkDebugUtilsMessageTypeFlagBitsEXT EnabledDebugMessageTypes {
			get;
			set {
				if (Engine3.WasVulkanSetup) { Logger.Warn("Attempted to set EnabledDebugMessageTypes too late"); }
				field = value;
			}
		} = VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeGeneralBitExt |
			VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeValidationBitExt |
			VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypePerformanceBitExt |
			VkDebugUtilsMessageTypeFlagBitsEXT.DebugUtilsMessageTypeDeviceAddressBindingBitExt;

		/// <summary>
		/// Helper method for extracting version information out of a packed uint following Vulkan's Version Specification.<br/><br/>
		/// The variant is packed into bits 31-29.<br/>
		/// The major version is packed into bits 28-22.<br/>
		/// The minor version number is packed into bits 21-12.<br/>
		/// The patch version number is packed into bits 11-0.<br/>
		/// </summary>
		/// <param name="version"> Packed integer containing the other 'out' parameters </param>
		/// <param name="variant"> 3-bit integer </param>
		/// <param name="major"> 7-bit integer </param>
		/// <param name="minor"> 10-bit integer </param>
		/// <param name="patch"> 12-bit integer </param>
		/// <seealso href="https://docs.vulkan.org/spec/latest/chapters/extensions.html#extendingvulkan-coreversions">Relevant Vulkan Specification</seealso>
		[Pure]
		public static void GetApiVersion(uint version, out byte variant, out byte major, out ushort minor, out ushort patch) {
			variant = (byte)(version >> 29);
			major = (byte)((version >> 22) & 0x7FU);
			minor = (ushort)((version >> 12) & 0x3FFU);
			patch = (ushort)(version & 0xFFFU);
		}

		[MustUseReturnValue]
		public static VkInstance CreateVulkanInstance(string appName, string engineName, Version4 gameVersion, Version4 engineVersion) {
			VkApplicationInfo vkApplicationInfo = new() {
					pApplicationName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(appName))),
					applicationVersion = gameVersion.Packed,
					pEngineName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(engineName))),
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

		[MustUseReturnValue]
		public static ReadOnlySpan<VkExtensionProperties> EnumerateInstanceExtensionProperties() {
			uint extensionCount;
			Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, null);

			if (extensionCount == 0) { return ReadOnlySpan<VkExtensionProperties>.Empty; }

			VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
			fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
				Vk.EnumerateInstanceExtensionProperties(null, &extensionCount, extensionPropertiesPtr);
				return extensionProperties;
			}
		}

		[MustUseReturnValue]
		public static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) =>
				Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR vkSurface) != VkResult.Success ? throw new VulkanException("Failed to create surface") : vkSurface;

		public static void CreateLogicalDevice(PhysicalGpu physicalGpu, out VkDevice vkLogicalDevice, out VkQueue vkPresentQueue) {
			QueueFamilyIndices queueFamilyIndices = physicalGpu.QueueFamilyIndices;

			HashSet<uint> uniqueQueueFamilies = [ queueFamilyIndices.GraphicsFamily, queueFamilyIndices.PresentFamily, ];
			List<VkDeviceQueueCreateInfo> queueCreateInfos = new();

			float queuePriority = 1f;

			// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator // CS0212
			foreach (uint queueFamily in uniqueQueueFamilies) { queueCreateInfos.Add(new() { queueFamilyIndex = queueFamily, queueCount = 1, pQueuePriorities = &queuePriority, }); }

			VkPhysicalDeviceFeatures deviceFeatures = new(); // ???

			IntPtr requiredDeviceExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(RequiredDeviceExtensions));

#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(CollectionsMarshal.AsSpan(RequiredValidationLayers));
#endif

			fixed (VkDeviceQueueCreateInfo* queueCreateInfosPtr = CollectionsMarshal.AsSpan(queueCreateInfos)) {
				VkDeviceCreateInfo deviceCreateInfo = new() {
						pQueueCreateInfos = queueCreateInfosPtr,
						queueCreateInfoCount = (uint)queueCreateInfos.Count,
						pEnabledFeatures = &deviceFeatures,
						ppEnabledExtensionNames = (byte**)requiredDeviceExtensionsPtr,
						enabledExtensionCount = (uint)RequiredDeviceExtensions.Count,
#if DEBUG // https://docs.vulkan.org/spec/latest/appendices/legacy.html#legacy-devicelayers
#pragma warning disable CS0618 // Type or member is obsolete
						enabledLayerCount = (uint)RequiredValidationLayers.Count,
						ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				};

				VkDevice logicalDevice;
				if (Vk.CreateDevice(physicalGpu.VkPhysicalDevice, &deviceCreateInfo, null, &logicalDevice) != VkResult.Success) { throw new VulkanException("Failed to create logical device"); }
				vkLogicalDevice = logicalDevice;
			}

			MarshalTk.FreeStringArrayCoTaskMem(requiredDeviceExtensionsPtr, RequiredDeviceExtensions.Count);

#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, RequiredValidationLayers.Count);
#endif

			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndices.PresentFamily, };

			VkQueue presentQueue;
			Vk.GetDeviceQueue2(vkLogicalDevice, &deviceQueueInfo2, &presentQueue);
			vkPresentQueue = presentQueue;
		}

		[MustUseReturnValue] public static VkPhysicalDevice[] CreatePhysicalDevices(VkInstance vkInstance) => EnumeratePhysicalDevices(vkInstance).ToArray();

		[MustUseReturnValue]
		public static PhysicalGpu[] GetValidGpus(VkPhysicalDevice[] vkPhysicalDevices, VkSurfaceKHR vkSurface, IsPhysicalDeviceSuitable isPhysicalDeviceSuitable) {
			List<PhysicalGpu> gpus = new();

			// ReSharper disable once LoopCanBeConvertedToQuery // don't think it can?
			foreach (VkPhysicalDevice device in vkPhysicalDevices) {
				VkPhysicalDeviceProperties2 vkPhysicalDeviceProperties2 = GetPhysicalDeviceProperties(device);
				VkPhysicalDeviceFeatures2 vkPhysicalDeviceFeatures2 = GetPhysicalDeviceFeatures(device);
				if (!isPhysicalDeviceSuitable(vkPhysicalDeviceProperties2, vkPhysicalDeviceFeatures2)) { continue; }

				VkExtensionProperties[] vkExtensionProperties = EnumeratePhysicalDeviceExtensionProperties(device).ToArray();
				if (!CheckDeviceExtensionSupport(vkExtensionProperties)) { continue; }

				FindQueueFamilies(device, vkSurface, out uint? graphicsFamily, out uint? presentFamily);
				if (graphicsFamily == null || presentFamily == null) { continue; }

				SwapChainSupportInfo swapChainSupportInfo = QuerySwapChainSupport(device, vkSurface);
				if (swapChainSupportInfo.VkSurfaceFormat.Length == 0 || swapChainSupportInfo.VkPresentMode.Length == 0) { continue; }

				gpus.Add(new(device, vkPhysicalDeviceProperties2, vkPhysicalDeviceFeatures2, vkExtensionProperties, new(graphicsFamily.Value, presentFamily.Value), swapChainSupportInfo));
			}

			return gpus.ToArray();

			static bool CheckDeviceExtensionSupport(VkExtensionProperties[] vkExtensionProperties) {
				if (vkExtensionProperties.Length == 0) { throw new VulkanException("Could not find any device extension properties"); }

				foreach (string wantedExtension in RequiredDeviceExtensions) {
					bool layerFound = false;

					foreach (VkExtensionProperties extensionProperties in vkExtensionProperties) {
						ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
						if (Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension) {
							layerFound = true;
							break;
						}
					}

					if (!layerFound) { return false; }
				}

				return true;
			}
		}

		[MustUseReturnValue]
		public static PhysicalGpu? PickBestGpu(PhysicalGpu[] gpus, RateGpuSuitability rateGpuSuitability) {
			PhysicalGpu? bestDevice = null;
			int bestDeviceScore = 0;

			foreach (PhysicalGpu device in gpus) {
				int score = rateGpuSuitability(device);
				if (score > bestDeviceScore) {
					bestDevice = device;
					bestDeviceScore = score;
				}
			}

			return bestDevice;
		}

		public static void PrintGpus(PhysicalGpu[] gpus, bool verbose) {
			Logger.Debug("The following GPUs are available:");
			foreach (PhysicalGpu gpu in gpus) {
				if (verbose) {
					foreach (string str in gpu.GetVerboseDescription()) { Logger.Debug(str); }
				} else {
					Logger.Debug($"- {gpu.GetSimpleDescription()}"); //
				}
			}
		}

		public static void CreateSwapChain(WindowHandle windowHandle, VkSurfaceKHR vkSurface, PhysicalGpu physicalGpu, VkDevice vkLogicalDevice, out VkSwapchainKHR vkSwapChain, out VkSurfaceFormat2KHR vkSurfaceFormat,
			out VkExtent2D vkExtent, VkSurfaceTransformFlagBitsKHR? vkSurfaceTransform = null) {
			vkSurfaceFormat = ChooseSwapSurfaceFormat(physicalGpu.SwapChainSupportInfo.VkSurfaceFormat) ?? throw new VulkanException("Could not find any valid surface formats");

			VkPresentModeKHR vkPresentMode = ChooseSwapPresentMode(physicalGpu.SwapChainSupportInfo.VkPresentMode);
			vkExtent = ChooseSwapExtent(windowHandle, physicalGpu.SwapChainSupportInfo.VkCapabilities);
			VkSurfaceCapabilitiesKHR capabilities = physicalGpu.SwapChainSupportInfo.VkCapabilities.surfaceCapabilities;

			VkSharingMode imageSharingMode;
			uint queueFamilyIndexCount;
			uint[]? queueFamilyIndices;

			// https://vulkan-tutorial.com/Drawing_a_triangle/Presentation/Swap_chain#Creating_the_swap_chain - "Therefore it is recommended to request at least one more image than the minimum"
			uint imageCount = capabilities.minImageCount + 1;
			if (capabilities.maxImageCount > 0 && imageCount > capabilities.maxImageCount) { imageCount = capabilities.maxImageCount; }

			if (physicalGpu.QueueFamilyIndices.GraphicsFamily != physicalGpu.QueueFamilyIndices.PresentFamily) {
				imageSharingMode = VkSharingMode.SharingModeConcurrent;
				queueFamilyIndexCount = 2;
				queueFamilyIndices = [ physicalGpu.QueueFamilyIndices.GraphicsFamily, physicalGpu.QueueFamilyIndices.PresentFamily, ];
			} else {
				imageSharingMode = VkSharingMode.SharingModeExclusive;
				queueFamilyIndexCount = 0;
				queueFamilyIndices = null;
			}

			fixed (uint* pQueueFamilyIndicesPtr = queueFamilyIndices) {
				VkSwapchainCreateInfoKHR createInfo = new() {
						surface = vkSurface,
						imageFormat = vkSurfaceFormat.surfaceFormat.format,
						imageColorSpace = vkSurfaceFormat.surfaceFormat.colorSpace,
						imageExtent = vkExtent,
						minImageCount = imageCount,
						imageArrayLayers = 1,
						imageUsage = VkImageUsageFlagBits.ImageUsageColorAttachmentBit,
						imageSharingMode = imageSharingMode,
						queueFamilyIndexCount = queueFamilyIndexCount,
						pQueueFamilyIndices = pQueueFamilyIndicesPtr,
						preTransform = vkSurfaceTransform ?? physicalGpu.SwapChainSupportInfo.VkCapabilities.surfaceCapabilities.currentTransform,
						compositeAlpha = VkCompositeAlphaFlagBitsKHR.CompositeAlphaOpaqueBitKhr,
						presentMode = vkPresentMode,
						clipped = (int)Vk.True,
						oldSwapchain = VkSwapchainKHR.Zero,
				};

				VkSwapchainKHR swapChain;
				if (Vk.CreateSwapchainKHR(vkLogicalDevice, &createInfo, null, &swapChain) != VkResult.Success) { throw new VulkanException("Failed to create swap chain"); }
				vkSwapChain = swapChain;
			}
		}

		[MustUseReturnValue]
		public static VkImage[] GetSwapChainImages(VkDevice vkLogicalDevice, VkSwapchainKHR vkSwapChain) {
			uint swapChainImagedCount;
			Vk.GetSwapchainImagesKHR(vkLogicalDevice, vkSwapChain, &swapChainImagedCount, null);

			VkImage[] swapChainImages = new VkImage[swapChainImagedCount];
			fixed (VkImage* swapChainImagesPtr = swapChainImages) {
				Vk.GetSwapchainImagesKHR(vkLogicalDevice, vkSwapChain, &swapChainImagedCount, swapChainImagesPtr);
				return swapChainImages;
			}
		}

		public static VkImageView[] CreateImageViews(VkDevice vkLogicalDevice, VkImage[] vkSwapChainImages, VkFormat vkSwapChainFormat) {
			VkImageView[] vkImageViews = new VkImageView[vkSwapChainImages.Length];

			fixed (VkImageView* vkImageViewsPtr = vkImageViews) {
				// ReSharper disable once LoopCanBeConvertedToQuery // nope
				for (int i = 0; i < vkSwapChainImages.Length; i++) {
					VkImageViewCreateInfo createInfo = new() {
							image = vkSwapChainImages[i],
							viewType = VkImageViewType.ImageViewType2d,
							format = vkSwapChainFormat,
							components = new() {
									r = VkComponentSwizzle.ComponentSwizzleIdentity,
									g = VkComponentSwizzle.ComponentSwizzleIdentity,
									b = VkComponentSwizzle.ComponentSwizzleIdentity,
									a = VkComponentSwizzle.ComponentSwizzleIdentity,
							},
							subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
					};

					if (Vk.CreateImageView(vkLogicalDevice, &createInfo, null, &vkImageViewsPtr[i]) != VkResult.Success) { throw new VulkanException("Failed to create image views"); }
				}
			}

			return vkImageViews;
		}

		public static bool CheckSupportForRequiredInstanceExtensions() {
			ReadOnlySpan<VkExtensionProperties> extensionProperties = EnumerateInstanceExtensionProperties();
			if (extensionProperties.Length == 0) { throw new VulkanException("Could not find any instance extension properties"); }

			Logger.Debug("The following instance extensions are available:");
			foreach (VkExtensionProperties extensionProperty in extensionProperties) {
				ReadOnlySpan<byte> extensionName = extensionProperty.extensionName;
				Logger.Debug($"- {Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)])}");
			}

			foreach (string wantedExtension in RequiredInstanceExtensions) {
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

		[MustUseReturnValue]
		private static SwapChainSupportInfo QuerySwapChainSupport(VkPhysicalDevice vkPhysicalDevice, VkSurfaceKHR vkSurface) {
			VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo2 = new() { surface = vkSurface, };
			VkSurfaceCapabilities2KHR surfaceCapabilities2 = new();

			Vk.GetPhysicalDeviceSurfaceCapabilities2KHR(vkPhysicalDevice, &surfaceInfo2, &surfaceCapabilities2);

			VkSurfaceFormat2KHR[] surfaceFormats = GetPhysicalDeviceSurfaceFormats(vkPhysicalDevice, surfaceInfo2);
			if (surfaceFormats.Length == 0) { throw new Engine3Exception("No surface formats found"); }

			VkPresentModeKHR[] presentModes = GetPhysicalDeviceSurfacePresentModes(vkPhysicalDevice, vkSurface);
			return presentModes.Length == 0 ? throw new Engine3Exception("No present modes found") : new(surfaceCapabilities2, surfaceFormats, presentModes);
		}

		[MustUseReturnValue]
		private static VkSurfaceFormat2KHR[] GetPhysicalDeviceSurfaceFormats(VkPhysicalDevice vkPhysicalDevice, VkPhysicalDeviceSurfaceInfo2KHR vkSurfaceInfo) {
			uint formatCount;
			Vk.GetPhysicalDeviceSurfaceFormats2KHR(vkPhysicalDevice, &vkSurfaceInfo, &formatCount, null);
			if (formatCount == 0) { return Array.Empty<VkSurfaceFormat2KHR>(); }

			VkSurfaceFormat2KHR[] surfaceFormats = new VkSurfaceFormat2KHR[formatCount];
			for (int i = 0; i < formatCount; i++) { surfaceFormats[i] = new(); }

			fixed (VkSurfaceFormat2KHR* surfaceFormatsPtr = surfaceFormats) {
				Vk.GetPhysicalDeviceSurfaceFormats2KHR(vkPhysicalDevice, &vkSurfaceInfo, &formatCount, surfaceFormatsPtr);
				return surfaceFormats;
			}
		}

		[MustUseReturnValue]
		private static VkSurfaceFormat2KHR? ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormat2KHR> availableFormats) =>
				availableFormats.FirstOrDefault(static format => format.surfaceFormat is { format: VkFormat.FormatB8g8r8a8Srgb, colorSpace: VkColorSpaceKHR.ColorSpaceSrgbNonlinearKhr, });

		[MustUseReturnValue]
		private static VkPresentModeKHR ChooseSwapPresentMode(ReadOnlySpan<VkPresentModeKHR> availablePresentModes) =>
				availablePresentModes.FirstOrDefault(static presentMode => presentMode is VkPresentModeKHR.PresentModeMailboxKhr, VkPresentModeKHR.PresentModeFifoKhr);

		[MustUseReturnValue]
		private static VkExtent2D ChooseSwapExtent(WindowHandle windowHandle, VkSurfaceCapabilities2KHR vkSurfaceCapabilities2) {
			VkSurfaceCapabilitiesKHR capabilities = vkSurfaceCapabilities2.surfaceCapabilities;
			if (capabilities.currentExtent.width != uint.MaxValue) { return capabilities.currentExtent; }

			Toolkit.Window.GetFramebufferSize(windowHandle, out Vector2i framebufferSize);
			return new() {
					width = Math.Clamp((uint)framebufferSize.X, capabilities.minImageExtent.width, capabilities.maxImageExtent.width),
					height = Math.Clamp((uint)framebufferSize.Y, capabilities.minImageExtent.height, capabilities.maxImageExtent.height),
			};
		}

		[MustUseReturnValue]
		private static VkPresentModeKHR[] GetPhysicalDeviceSurfacePresentModes(VkPhysicalDevice vkPhysicalDevice, VkSurfaceKHR vkSurface) {
			uint presentModeCount;
			Vk.GetPhysicalDeviceSurfacePresentModesKHR(vkPhysicalDevice, vkSurface, &presentModeCount, null);
			if (presentModeCount == 0) { return Array.Empty<VkPresentModeKHR>(); }

			VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
			fixed (VkPresentModeKHR* presentModesPtr = presentModes) {
				Vk.GetPhysicalDeviceSurfacePresentModesKHR(vkPhysicalDevice, vkSurface, &presentModeCount, presentModesPtr);
				return presentModes;
			}
		}

		[MustUseReturnValue]
		private static bool GetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice vkPhysicalDevice, uint index, VkSurfaceKHR vkSurface) {
			int presentSupport;
			Vk.GetPhysicalDeviceSurfaceSupportKHR(vkPhysicalDevice, index, vkSurface, &presentSupport);
			return presentSupport == 1;
		}

		[MustUseReturnValue]
		private static VkPhysicalDeviceProperties2 GetPhysicalDeviceProperties(VkPhysicalDevice vkPhysicalDevice) {
			VkPhysicalDeviceProperties2 deviceProperties2 = new();
			Vk.GetPhysicalDeviceProperties2(vkPhysicalDevice, &deviceProperties2);
			return deviceProperties2;
		}

		[MustUseReturnValue]
		private static VkPhysicalDeviceFeatures2 GetPhysicalDeviceFeatures(VkPhysicalDevice vkPhysicalDevice) {
			VkPhysicalDeviceFeatures2 deviceFeatures2 = new();
			Vk.GetPhysicalDeviceFeatures2(vkPhysicalDevice, &deviceFeatures2);
			return deviceFeatures2;
		}

		[MustUseReturnValue]
		private static ReadOnlySpan<VkQueueFamilyProperties2> GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice device) {
			uint queueFamilyPropertyCount = 0;
			Vk.GetPhysicalDeviceQueueFamilyProperties2(device, &queueFamilyPropertyCount, null);

			if (queueFamilyPropertyCount == 0) { return ReadOnlySpan<VkQueueFamilyProperties2>.Empty; }

			VkQueueFamilyProperties2[] queueFamilyProperties = new VkQueueFamilyProperties2[queueFamilyPropertyCount];
			for (int i = 0; i < queueFamilyPropertyCount; i++) { queueFamilyProperties[i] = new() { sType = VkStructureType.StructureTypeQueueFamilyProperties2, }; }

			fixed (VkQueueFamilyProperties2* queueFamilyPropertiesPtr = queueFamilyProperties) {
				Vk.GetPhysicalDeviceQueueFamilyProperties2(device, &queueFamilyPropertyCount, queueFamilyPropertiesPtr);
				return queueFamilyProperties;
			}
		}

		[MustUseReturnValue]
		private static ReadOnlySpan<VkExtensionProperties> EnumeratePhysicalDeviceExtensionProperties(VkPhysicalDevice vkPhysicalDevice) {
			uint extensionCount;
			Vk.EnumerateDeviceExtensionProperties(vkPhysicalDevice, null, &extensionCount, null);

			if (extensionCount == 0) { return ReadOnlySpan<VkExtensionProperties>.Empty; }

			VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
			fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
				Vk.EnumerateDeviceExtensionProperties(vkPhysicalDevice, null, &extensionCount, extensionPropertiesPtr);
				return extensionProperties;
			}
		}

		[MustUseReturnValue]
		private static ReadOnlySpan<VkPhysicalDevice> EnumeratePhysicalDevices(VkInstance vkInstance) {
			uint deviceCount;
			Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, null);

			if (deviceCount == 0) { return ReadOnlySpan<VkPhysicalDevice>.Empty; }

			VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
			fixed (VkPhysicalDevice* physicalDevicesPtr = physicalDevices) {
				Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, physicalDevicesPtr);
				return physicalDevices;
			}
		}

		private static void FindQueueFamilies(VkPhysicalDevice vkPhysicalDevice, VkSurfaceKHR vkSurface, out uint? graphicsFamily, out uint? presentFamily) {
			graphicsFamily = null;
			presentFamily = null;

			ReadOnlySpan<VkQueueFamilyProperties2> queueFamilies = GetPhysicalDeviceQueueFamilyProperties(vkPhysicalDevice);
			uint i = 0;
			foreach (VkQueueFamilyProperties2 queueFamilyProperties2 in queueFamilies) {
				VkQueueFamilyProperties queueFamilyProperties = queueFamilyProperties2.queueFamilyProperties;
				if ((queueFamilyProperties.queueFlags & VkQueueFlagBits.QueueGraphicsBit) != 0) { graphicsFamily = i; }
				if (GetPhysicalDeviceSurfaceSupportKHR(vkPhysicalDevice, i, vkSurface)) { presentFamily = i; }

				if (graphicsFamily != null && presentFamily != null) { break; }

				i++;
			}
		}

		public delegate bool IsPhysicalDeviceSuitable(VkPhysicalDeviceProperties2 vkPhysicalDeviceProperties2, VkPhysicalDeviceFeatures2 vkPhysicalDeviceFeatures2);
		public delegate int RateGpuSuitability(PhysicalGpu physicalGpu);
	}
}