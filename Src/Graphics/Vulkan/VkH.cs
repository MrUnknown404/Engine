using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
using shaderc;
using SpirVCompiler = shaderc.Compiler;
using SpirVResult = shaderc.Result;

namespace Engine3.Graphics.Vulkan {
	public static unsafe partial class VkH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
		public static VkInstance CreateVulkanInstance(GameClient gameClient, string engineName, Version4 gameVersion, Version4 engineVersion) {
			string[] requiredInstanceExtensions = gameClient.VkGetInstanceExtensions();
			string[] requiredValidationLayers = gameClient.VkGetRequiredValidationLayers();

			VkApplicationInfo vkApplicationInfo = new() {
					pApplicationName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(gameClient.Name))),
					applicationVersion = gameVersion.Packed,
					pEngineName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(engineName))),
					engineVersion = engineVersion.Packed,
					apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
			};

			IntPtr requiredExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredInstanceExtensions);
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredValidationLayers);

			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(gameClient);
#endif

			VkInstanceCreateInfo vkCreateInfo = new() {
					pApplicationInfo = &vkApplicationInfo,
#if DEBUG
					pNext = &messengerCreateInfo,
					enabledLayerCount = (uint)requiredValidationLayers.Length,
					ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#endif
					enabledExtensionCount = (uint)requiredInstanceExtensions.Length,
					ppEnabledExtensionNames = (byte**)requiredExtensionsPtr,
			};

			VkInstance vkInstance;
			if (Vk.CreateInstance(&vkCreateInfo, null, &vkInstance) != VkResult.Success) { throw new VulkanException("Failed to create Vulkan instance"); }

			MarshalTk.FreeStringArrayCoTaskMem(requiredExtensionsPtr, requiredInstanceExtensions.Length);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

			return vkInstance;
		}

		[MustUseReturnValue]
		public static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) =>
				Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR vkSurface) != VkResult.Success ? throw new VulkanException("Failed to create surface") : vkSurface;

		[MustUseReturnValue]
		public static VkDevice CreateLogicalDevice(PhysicalGpu physicalGpu, string[] requiredDeviceExtensions, string[] requiredValidationLayers) {
			QueueFamilyIndices queueFamilyIndices = physicalGpu.QueueFamilyIndices;

			List<VkDeviceQueueCreateInfo> queueCreateInfos = new();
			float queuePriority = 1f;

			// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator // CS0212
			foreach (uint queueFamily in new HashSet<uint> { queueFamilyIndices.GraphicsFamily, queueFamilyIndices.PresentFamily, queueFamilyIndices.TransferFamily, }) {
				queueCreateInfos.Add(new() { queueFamilyIndex = queueFamily, queueCount = 1, pQueuePriorities = &queuePriority, });
			}

			VkPhysicalDeviceDynamicRenderingFeaturesKHR dynamicRenderingFeatures = new() { dynamicRendering = (int)Vk.True, };
			VkPhysicalDeviceFeatures deviceFeatures = new(); // ???

			IntPtr requiredDeviceExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredDeviceExtensions);
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredValidationLayers);
#endif

			VkDevice logicalDevice;
			fixed (VkDeviceQueueCreateInfo* queueCreateInfosPtr = CollectionsMarshal.AsSpan(queueCreateInfos)) {
				VkDeviceCreateInfo deviceCreateInfo = new() {
						pNext = &dynamicRenderingFeatures,
						pQueueCreateInfos = queueCreateInfosPtr,
						queueCreateInfoCount = (uint)queueCreateInfos.Count,
						pEnabledFeatures = &deviceFeatures,
						ppEnabledExtensionNames = (byte**)requiredDeviceExtensionsPtr,
						enabledExtensionCount = (uint)requiredDeviceExtensions.Length,
#if DEBUG // https://docs.vulkan.org/spec/latest/appendices/legacy.html#legacy-devicelayers
#pragma warning disable CS0618 // Type or member is obsolete
						enabledLayerCount = (uint)requiredValidationLayers.Length,
						ppEnabledLayerNames = (byte**)requiredValidationLayersPtr,
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				};

				if (Vk.CreateDevice(physicalGpu.VkPhysicalDevice, &deviceCreateInfo, null, &logicalDevice) != VkResult.Success) { throw new VulkanException("Failed to create logical device"); }
			}

			MarshalTk.FreeStringArrayCoTaskMem(requiredDeviceExtensionsPtr, requiredDeviceExtensions.Length);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

			return logicalDevice;
		}

		[MustUseReturnValue]
		public static PhysicalGpu[] GetValidGpus(VkPhysicalDevice[] vkPhysicalDevices, VkSurfaceKHR vkSurface, IsPhysicalDeviceSuitable isPhysicalDeviceSuitable, string[] requiredDeviceExtensions) {
			List<PhysicalGpu> gpus = new();

			// ReSharper disable once LoopCanBeConvertedToQuery // don't think it can?
			foreach (VkPhysicalDevice device in vkPhysicalDevices) {
				VkPhysicalDeviceProperties2 vkPhysicalDeviceProperties2 = GetPhysicalDeviceProperties(device);
				VkPhysicalDeviceFeatures2 vkPhysicalDeviceFeatures2 = GetPhysicalDeviceFeatures(device);
				if (!isPhysicalDeviceSuitable(vkPhysicalDeviceProperties2, vkPhysicalDeviceFeatures2)) { continue; }

				VkExtensionProperties[] vkExtensionProperties = GetPhysicalDeviceExtensionProperties(device);
				if (vkExtensionProperties.Length == 0) { throw new VulkanException("Could not find any device extension properties"); }
				if (!CheckDeviceExtensionSupport(vkExtensionProperties, requiredDeviceExtensions)) { continue; }

				FindQueueFamilies(device, vkSurface, out uint? graphicsFamily, out uint? presentFamily, out uint? transferFamily);
				if (graphicsFamily == null || presentFamily == null || transferFamily == null) { continue; }

				SwapChainSupportInfo swapChainSupportInfo = QuerySwapChainSupport(device, vkSurface);
				if (swapChainSupportInfo.VkSurfaceFormats.Length == 0 || swapChainSupportInfo.VkPresentModes.Length == 0) { continue; }

				gpus.Add(new(device, vkPhysicalDeviceProperties2, vkPhysicalDeviceFeatures2, vkExtensionProperties, new(graphicsFamily.Value, presentFamily.Value, transferFamily.Value)));
			}

			return gpus.ToArray();

			// idk. not sure if i like local methods

			[MustUseReturnValue]
			static VkPhysicalDeviceProperties2 GetPhysicalDeviceProperties(VkPhysicalDevice vkPhysicalDevice) {
				VkPhysicalDeviceProperties2 deviceProperties2 = new();
				Vk.GetPhysicalDeviceProperties2(vkPhysicalDevice, &deviceProperties2);
				return deviceProperties2;
			}

			[MustUseReturnValue]
			static VkPhysicalDeviceFeatures2 GetPhysicalDeviceFeatures(VkPhysicalDevice vkPhysicalDevice) {
				VkPhysicalDeviceFeatures2 deviceFeatures2 = new();
				Vk.GetPhysicalDeviceFeatures2(vkPhysicalDevice, &deviceFeatures2);
				return deviceFeatures2;
			}

			[MustUseReturnValue]
			static VkExtensionProperties[] GetPhysicalDeviceExtensionProperties(VkPhysicalDevice vkPhysicalDevice) {
				uint extensionCount;
				Vk.EnumerateDeviceExtensionProperties(vkPhysicalDevice, null, &extensionCount, null);

				if (extensionCount == 0) { return Array.Empty<VkExtensionProperties>(); }

				VkExtensionProperties[] extensionProperties = new VkExtensionProperties[extensionCount];
				fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties) {
					Vk.EnumerateDeviceExtensionProperties(vkPhysicalDevice, null, &extensionCount, extensionPropertiesPtr);
					return extensionProperties;
				}
			}

			[MustUseReturnValue]
			static bool GetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice vkPhysicalDevice, uint index, VkSurfaceKHR vkSurface) {
				int presentSupport;
				Vk.GetPhysicalDeviceSurfaceSupportKHR(vkPhysicalDevice, index, vkSurface, &presentSupport);
				return presentSupport == 1;
			}

			[MustUseReturnValue]
			static VkQueueFamilyProperties2[] GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice device) {
				uint queueFamilyPropertyCount = 0;
				Vk.GetPhysicalDeviceQueueFamilyProperties2(device, &queueFamilyPropertyCount, null);

				if (queueFamilyPropertyCount == 0) { return Array.Empty<VkQueueFamilyProperties2>(); }

				VkQueueFamilyProperties2[] queueFamilyProperties = new VkQueueFamilyProperties2[queueFamilyPropertyCount];
				for (int i = 0; i < queueFamilyPropertyCount; i++) { queueFamilyProperties[i] = new() { sType = VkStructureType.StructureTypeQueueFamilyProperties2, }; }

				fixed (VkQueueFamilyProperties2* queueFamilyPropertiesPtr = queueFamilyProperties) {
					Vk.GetPhysicalDeviceQueueFamilyProperties2(device, &queueFamilyPropertyCount, queueFamilyPropertiesPtr);
					return queueFamilyProperties;
				}
			}

			[MustUseReturnValue]
			static bool CheckDeviceExtensionSupport(VkExtensionProperties[] vkExtensionProperties, string[] requiredDeviceExtensions) {
				foreach (string wantedExtension in requiredDeviceExtensions) {
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

			static void FindQueueFamilies(VkPhysicalDevice vkPhysicalDevice, VkSurfaceKHR vkSurface, out uint? graphicsFamily, out uint? presentFamily, out uint? transferFamily) {
				graphicsFamily = null;
				presentFamily = null;
				transferFamily = null;

				VkQueueFamilyProperties2[] queueFamilies = GetPhysicalDeviceQueueFamilyProperties(vkPhysicalDevice);
				uint i = 0;
				foreach (VkQueueFamilyProperties2 queueFamilyProperties2 in queueFamilies) {
					VkQueueFamilyProperties queueFamilyProperties = queueFamilyProperties2.queueFamilyProperties;
					if ((queueFamilyProperties.queueFlags & VkQueueFlagBits.QueueGraphicsBit) != 0) { graphicsFamily = i; }
					if ((queueFamilyProperties.queueFlags & VkQueueFlagBits.QueueTransferBit) != 0) { transferFamily = i; }
					if (GetPhysicalDeviceSurfaceSupportKHR(vkPhysicalDevice, i, vkSurface)) { presentFamily = i; }

					if (graphicsFamily != null && presentFamily != null && transferFamily != null) { break; }

					i++;
				}
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

		[MustUseReturnValue]
		public static SwapChain CreateSwapChain(WindowHandle windowHandle, VkSurfaceKHR vkSurface, PhysicalGpu physicalGpu, VkDevice vkLogicalDevice, VkPresentModeKHR vkPresentMode = VkPresentModeKHR.PresentModeMailboxKhr,
			VkSurfaceTransformFlagBitsKHR? vkSurfaceTransform = null, VkSwapchainKHR? oldSwapChain = null) {
			SwapChainSupportInfo swapChainSupportInfo = QuerySwapChainSupport(physicalGpu.VkPhysicalDevice, vkSurface);
			QueueFamilyIndices queueFamilyIndices = physicalGpu.QueueFamilyIndices;
			VkSurfaceCapabilitiesKHR surfaceCapabilities = swapChainSupportInfo.VkCapabilities.surfaceCapabilities;

			VkSurfaceFormat2KHR vkSurfaceFormat = ChooseSwapSurfaceFormat(swapChainSupportInfo.VkSurfaceFormats) ?? throw new VulkanException("Could not find any valid surface formats");
			VkFormat vkSwapChainImageFormat = vkSurfaceFormat.surfaceFormat.format;
			VkPresentModeKHR chosenPresentMode = ChooseSwapPresentMode(swapChainSupportInfo.VkPresentModes, vkPresentMode);
			VkExtent2D vkSwapChainExtent = ChooseSwapExtent(windowHandle, surfaceCapabilities);

			// https://vulkan-tutorial.com/Drawing_a_triangle/Presentation/Swap_chain#Creating_the_swap_chain - "Therefore it is recommended to request at least one more image than the minimum"
			uint imageCount = surfaceCapabilities.minImageCount + 1;
			if (surfaceCapabilities.maxImageCount > 0 && imageCount > surfaceCapabilities.maxImageCount) { imageCount = surfaceCapabilities.maxImageCount; }

			VkSharingMode imageSharingMode;
			uint queueFamilyIndexCount;
			uint[] queueFamilyIndicesArray;

			HashSet<uint> hashSet = [ queueFamilyIndices.GraphicsFamily, queueFamilyIndices.TransferFamily, queueFamilyIndices.PresentFamily, ];

			if (hashSet.Count != 1) {
				imageSharingMode = VkSharingMode.SharingModeConcurrent;
				queueFamilyIndexCount = (uint)hashSet.Count;
				queueFamilyIndicesArray = hashSet.ToArray();
			} else {
				imageSharingMode = VkSharingMode.SharingModeExclusive;
				queueFamilyIndexCount = 0;
				queueFamilyIndicesArray = Array.Empty<uint>();
			}

			VkSwapchainKHR vkSwapChain;
			fixed (uint* pQueueFamilyIndicesPtr = queueFamilyIndicesArray) {
				VkSwapchainCreateInfoKHR createInfo = new() {
						surface = vkSurface,
						imageFormat = vkSwapChainImageFormat,
						imageColorSpace = vkSurfaceFormat.surfaceFormat.colorSpace,
						imageExtent = vkSwapChainExtent,
						minImageCount = imageCount,
						imageArrayLayers = 1,
						imageUsage = VkImageUsageFlagBits.ImageUsageColorAttachmentBit,
						imageSharingMode = imageSharingMode,
						queueFamilyIndexCount = queueFamilyIndexCount,
						pQueueFamilyIndices = queueFamilyIndicesArray.Length == 0 ? null : pQueueFamilyIndicesPtr,
						preTransform = vkSurfaceTransform ?? surfaceCapabilities.currentTransform,
						compositeAlpha = VkCompositeAlphaFlagBitsKHR.CompositeAlphaOpaqueBitKhr,
						presentMode = chosenPresentMode,
						clipped = (int)Vk.True,
						oldSwapchain = oldSwapChain ?? VkSwapchainKHR.Zero,
				};

				VkResult vkResult = Vk.CreateSwapchainKHR(vkLogicalDevice, &createInfo, null, &vkSwapChain);
				if (vkResult != VkResult.Success) { throw new VulkanException($"Failed to create swap chain. {vkResult}"); }
			}

			VkImage[] vkSwapChainImages = GetSwapChainImages(vkLogicalDevice, vkSwapChain);
			VkImageView[] vkSwapChainImageViews = CreateImageViews(vkLogicalDevice, vkSwapChainImages, vkSwapChainImageFormat);

			return new(vkSwapChain, vkSwapChainImages, vkSwapChainImageFormat, vkSwapChainExtent, vkSwapChainImageViews);

			[MustUseReturnValue]
			static VkSurfaceFormat2KHR? ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormat2KHR> availableFormats) =>
					availableFormats.FirstOrDefault(static format => format.surfaceFormat is { format: VkFormat.FormatB8g8r8a8Srgb, colorSpace: VkColorSpaceKHR.ColorSpaceSrgbNonlinearKhr, });

			[MustUseReturnValue]
			static VkPresentModeKHR ChooseSwapPresentMode(ReadOnlySpan<VkPresentModeKHR> availablePresentModes, VkPresentModeKHR vkPresentMode) =>
					availablePresentModes.FirstOrDefault(presentMode => presentMode == vkPresentMode, VkPresentModeKHR.PresentModeFifoKhr);

			[MustUseReturnValue]
			static VkExtent2D ChooseSwapExtent(WindowHandle windowHandle, VkSurfaceCapabilitiesKHR vkSurfaceCapabilities) {
				if (vkSurfaceCapabilities.currentExtent.width != uint.MaxValue) { return vkSurfaceCapabilities.currentExtent; }

				Toolkit.Window.GetFramebufferSize(windowHandle, out Vector2i framebufferSize);

				return new() {
						width = Math.Clamp((uint)framebufferSize.X, vkSurfaceCapabilities.minImageExtent.width, vkSurfaceCapabilities.maxImageExtent.width),
						height = Math.Clamp((uint)framebufferSize.Y, vkSurfaceCapabilities.minImageExtent.height, vkSurfaceCapabilities.maxImageExtent.height),
				};
			}

			[MustUseReturnValue]
			static VkImage[] GetSwapChainImages(VkDevice vkLogicalDevice, VkSwapchainKHR vkSwapChain) {
				uint swapChainImageCount;
				Vk.GetSwapchainImagesKHR(vkLogicalDevice, vkSwapChain, &swapChainImageCount, null);

				VkImage[] swapChainImages = new VkImage[swapChainImageCount];
				fixed (VkImage* swapChainImagesPtr = swapChainImages) {
					Vk.GetSwapchainImagesKHR(vkLogicalDevice, vkSwapChain, &swapChainImageCount, swapChainImagesPtr);
					return swapChainImages;
				}
			}

			[MustUseReturnValue]
			static VkImageView[] CreateImageViews(VkDevice vkLogicalDevice, VkImage[] vkSwapChainImages, VkFormat vkSwapChainFormat) {
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
		}

		[MustUseReturnValue]
		public static SwapChain RecreateSwapChain(WindowHandle windowHandle, VkSurfaceKHR vkSurface, PhysicalGpu selectedGpu, VkDevice vkLogicalDevice, SwapChain oldSwapChain) {
			Vk.DeviceWaitIdle(vkLogicalDevice);
			SwapChain newSwapChain = CreateSwapChain(windowHandle, vkSurface, selectedGpu, vkLogicalDevice, VkPresentModeKHR.PresentModeImmediateKhr, oldSwapChain: oldSwapChain.VkSwapChain);
			oldSwapChain.Destroy(vkLogicalDevice);
			return newSwapChain;
		}

		[MustUseReturnValue]
		public static bool CheckSupportForRequiredInstanceExtensions(VkExtensionProperties[] extensionProperties, string[] requiredInstanceExtensions) {
			foreach (string wantedExtension in requiredInstanceExtensions) {
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

		public static void CreateGraphicsPipeline(VkDevice vkLogicalDevice, VkFormat vkSwapChainImageFormat, VkPipelineShaderStageCreateInfo[] vkShaderStageCreateInfos, out VkPipeline vkGraphicsPipeline,
			out VkPipelineLayout vkPipelineLayout) {
			if (vkShaderStageCreateInfos.Length == 0) { throw new VulkanException($"{nameof(vkShaderStageCreateInfos)} cannot be empty"); }

			VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new() { topology = VkPrimitiveTopology.PrimitiveTopologyTriangleList, };
			VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new() { viewportCount = 1, scissorCount = 1, };
			VkPipelineRenderingCreateInfo renderingCreateInfo = new() { colorAttachmentCount = 1, pColorAttachmentFormats = &vkSwapChainImageFormat, };

			VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new() {
					depthClampEnable = (int)Vk.False,
					rasterizerDiscardEnable = (int)Vk.False,
					polygonMode = VkPolygonMode.PolygonModeFill,
					lineWidth = 1,
					cullMode = VkCullModeFlagBits.CullModeBackBit,
					frontFace = VkFrontFace.FrontFaceClockwise,
					depthBiasEnable = (int)Vk.False,
					depthBiasConstantFactor = 0,
					depthBiasClamp = 0,
					depthBiasSlopeFactor = 0,
			};

			VkPipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new() {
					sampleShadingEnable = (int)Vk.False,
					rasterizationSamples = VkSampleCountFlagBits.SampleCount1Bit,
					minSampleShading = 1,
					pSampleMask = null,
					alphaToCoverageEnable = (int)Vk.False,
					alphaToOneEnable = (int)Vk.False,
			};

			VkPipelineColorBlendAttachmentState colorBlendAttachmentState = new() {
					colorWriteMask = VkColorComponentFlagBits.ColorComponentRBit | VkColorComponentFlagBits.ColorComponentGBit | VkColorComponentFlagBits.ColorComponentBBit | VkColorComponentFlagBits.ColorComponentABit,
					blendEnable = (int)Vk.True,
					srcColorBlendFactor = VkBlendFactor.BlendFactorSrcAlpha,
					dstColorBlendFactor = VkBlendFactor.BlendFactorOneMinusSrcAlpha,
					colorBlendOp = VkBlendOp.BlendOpAdd,
					srcAlphaBlendFactor = VkBlendFactor.BlendFactorOne,
					dstAlphaBlendFactor = VkBlendFactor.BlendFactorZero,
					alphaBlendOp = VkBlendOp.BlendOpAdd,
			};

			VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new() { logicOpEnable = (int)Vk.False, logicOp = VkLogicOp.LogicOpCopy, attachmentCount = 1, pAttachments = &colorBlendAttachmentState, };
			colorBlendStateCreateInfo.blendConstants[0] = 0; // is there a better way of initializing this?
			colorBlendStateCreateInfo.blendConstants[1] = 0;
			colorBlendStateCreateInfo.blendConstants[2] = 0;
			colorBlendStateCreateInfo.blendConstants[3] = 0;

			VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new() { setLayoutCount = 0, pSetLayouts = null, pushConstantRangeCount = 0, pPushConstantRanges = null, };

			VkPipelineLayout pipelineLayout;
			if (Vk.CreatePipelineLayout(vkLogicalDevice, &pipelineLayoutCreateInfo, null, &pipelineLayout) != VkResult.Success) { throw new VulkanException("Failed to create pipeline layout"); }
			vkPipelineLayout = pipelineLayout;

			fixed (VkPipelineShaderStageCreateInfo* shaderStageCreateInfosPtr = vkShaderStageCreateInfos) {
				VkDynamicState[] dynamicStates = [ VkDynamicState.DynamicStateViewport, VkDynamicState.DynamicStateScissor, ]; // TODO allow this to be edited

				fixed (VkDynamicState* dynamicStatesPtr = dynamicStates) {
					VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new() { dynamicStateCount = (uint)dynamicStates.Length, pDynamicStates = dynamicStatesPtr, };
					VkVertexInputBindingDescription bindingDescription = TestVertex.GetBindingDescription();
					VkVertexInputAttributeDescription[] attributeDescriptions = TestVertex.GetAttributeDescriptions();

					fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions) {
						VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new() {
								vertexBindingDescriptionCount = 1,
								vertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
								pVertexBindingDescriptions = &bindingDescription,
								pVertexAttributeDescriptions = attributeDescriptionsPtr,
						};

						VkGraphicsPipelineCreateInfo pipelineCreateInfo = new() {
								pNext = &renderingCreateInfo,
								stageCount = (uint)vkShaderStageCreateInfos.Length,
								pStages = shaderStageCreateInfosPtr,
								pVertexInputState = &vertexInputStateCreateInfo,
								pInputAssemblyState = &inputAssemblyStateCreateInfo,
								pViewportState = &viewportStateCreateInfo,
								pRasterizationState = &rasterizationStateCreateInfo,
								pMultisampleState = &multisampleStateCreateInfo,
								pDepthStencilState = null,
								pColorBlendState = &colorBlendStateCreateInfo,
								pDynamicState = &dynamicStateCreateInfo,
								layout = pipelineLayout,
								basePipelineHandle = VkPipeline.Zero,
								basePipelineIndex = -1,
						};

						VkPipeline graphicsPipeline;
						if (Vk.CreateGraphicsPipelines(vkLogicalDevice, VkPipelineCache.Zero, 1, &pipelineCreateInfo, null, &graphicsPipeline) != VkResult.Success) {
							throw new VulkanException("Failed to create graphics pipeline");
						}

						vkGraphicsPipeline = graphicsPipeline;
					}
				}
			}
		}

		[MustUseReturnValue]
		public static VkQueue GetDeviceQueue(VkDevice vkLogicalDevice, uint queueFamilyIndex) {
			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
			VkQueue vkGraphicsQueue;
			Vk.GetDeviceQueue2(vkLogicalDevice, &deviceQueueInfo2, &vkGraphicsQueue);
			return vkGraphicsQueue;
		}

		[MustUseReturnValue]
		public static VkShaderModule? CreateShaderModule(VkDevice vkLogicalDevice, string fileName, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) {
			using Stream? stream = AssetH.GetAssetStream($"Shaders.{shaderLang.AssetFolderName}.{fileName}.{shaderType.FileExtension}{(shaderLang == ShaderLanguage.SpirV ? ".spv" : string.Empty)}", assembly);
			if (stream == null) {
				Logger.Error("Failed to create asset stream");
				return null;
			}

			switch (shaderLang) {
				case ShaderLanguage.Glsl or ShaderLanguage.Hlsl: {
					using SpirVCompiler spirvCompiler = new(new Options {
							SourceLanguage = shaderLang switch {
									ShaderLanguage.Glsl => SourceLanguage.Glsl,
									ShaderLanguage.Hlsl => SourceLanguage.Hlsl,
									ShaderLanguage.SpirV => throw new UnreachableException(),
									_ => throw new NotImplementedException(),
							},
					});

					using StreamReader streamReader = new(stream);
					using SpirVResult spirvResult = spirvCompiler.Compile(streamReader.ReadToEnd(), fileName, shaderType.ShaderKind);

					if (spirvResult.Status != Status.Success) {
						Logger.Error($"Failed to compile shader: {fileName}. {spirvResult.ErrorMessage}");
						return null;
					}

					VkShaderModuleCreateInfo createInfo = new() { codeSize = spirvResult.CodeLength, pCode = (uint*)spirvResult.CodePointer, };
					VkShaderModule vkShaderModule;
					return Vk.CreateShaderModule(vkLogicalDevice, &createInfo, null, &vkShaderModule) != VkResult.Success ? throw new VulkanException("Failed to create shader module") : vkShaderModule;
				}
				case ShaderLanguage.SpirV: {
					using BinaryReader reader = new(stream);
					byte[] data = reader.ReadBytes((int)stream.Length);

					fixed (byte* shaderCodePtr = data) {
						VkShaderModuleCreateInfo createInfo = new() { codeSize = (UIntPtr)data.Length, pCode = (uint*)shaderCodePtr, };
						VkShaderModule vkShaderModule;
						return Vk.CreateShaderModule(vkLogicalDevice, &createInfo, null, &vkShaderModule) != VkResult.Success ? throw new VulkanException("Failed to create shader module") : vkShaderModule;
					}
				}
				default: throw new ArgumentOutOfRangeException(nameof(shaderLang), shaderLang, null);
			}
		}

		[MustUseReturnValue]
		public static VkCommandPool CreateCommandPool(VkDevice vkLogicalDevice, VkCommandPoolCreateFlagBits vkCommandPoolCreateFlag, uint queueFamilyIndex) {
			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = vkCommandPoolCreateFlag, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool vkCommandPool;
			return Vk.CreateCommandPool(vkLogicalDevice, &commandPoolCreateInfo, null, &vkCommandPool) != VkResult.Success ? throw new VulkanException("Failed to create command pool") : vkCommandPool;
		}

		[MustUseReturnValue]
		public static VkCommandBuffer[] CreateCommandBuffers(VkDevice vkLogicalDevice, VkCommandPool vkCommandPool, uint count) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = vkCommandPool, level = VkCommandBufferLevel.CommandBufferLevelPrimary, commandBufferCount = count, };
			VkCommandBuffer[] vkCommandBuffers = new VkCommandBuffer[count];
			fixed (VkCommandBuffer* vkCommandBuffersPtr = vkCommandBuffers) {
				return Vk.AllocateCommandBuffers(vkLogicalDevice, &commandBufferAllocateInfo, vkCommandBuffersPtr) != VkResult.Success ? throw new VulkanException("Failed to create command buffer") : vkCommandBuffers;
			}
		}

		[MustUseReturnValue]
		public static VkSemaphore[] CreateSemaphores(VkDevice vkLogicalDevice, uint count) {
			VkSemaphore[] vkSemaphores = new VkSemaphore[count];
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();

			fixed (VkSemaphore* vkSemaphoresPtr = vkSemaphores) {
				for (int i = 0; i < count; i++) {
					if (Vk.CreateSemaphore(vkLogicalDevice, &semaphoreCreateInfo, null, &vkSemaphoresPtr[i]) != VkResult.Success) { throw new VulkanException("failed to create semaphore"); }
				}
			}

			return vkSemaphores;
		}

		[MustUseReturnValue]
		public static VkFence[] CreateFence(VkDevice vkLogicalDevice, uint count) {
			VkFence[] vkFences = new VkFence[count];
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };

			fixed (VkFence* vkFencesPtr = vkFences) {
				for (int i = 0; i < count; i++) {
					if (Vk.CreateFence(vkLogicalDevice, &fenceCreateInfo, null, &vkFencesPtr[i]) != VkResult.Success) { throw new VulkanException("Failed to create fence"); }
				}
			}

			return vkFences;
		}

		public static void SubmitCommandBufferQueue(VkQueue vkQueue, VkCommandBuffer vkCommandBuffer, VkSemaphore imageAvailable, VkSemaphore renderFinished, VkFence inFlight) {
			VkPipelineStageFlagBits[] waitStages = [ VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, ];

			fixed (VkPipelineStageFlagBits* waitStagesPtr = waitStages) {
				VkSubmitInfo vkSubmitInfo = new() { // TODO 2?
						waitSemaphoreCount = 1,
						pWaitSemaphores = &imageAvailable, // these can all be arrays if needed
						pWaitDstStageMask = waitStagesPtr,
						commandBufferCount = 1,
						pCommandBuffers = &vkCommandBuffer,
						signalSemaphoreCount = 1,
						pSignalSemaphores = &renderFinished,
				};

				SubmitQueue(vkQueue, [ vkSubmitInfo, ], inFlight);
			}
		}

		public static void SubmitQueue(VkQueue vkQueue, VkSubmitInfo[] vkSubmitInfos, VkFence vkFence) {
			fixed (VkSubmitInfo* vkSubmitInfosPtr = vkSubmitInfos) {
				if (Vk.QueueSubmit(vkQueue, (uint)vkSubmitInfos.Length, vkSubmitInfosPtr, vkFence) != VkResult.Success) { throw new VulkanException("Failed to submit queue"); } // TODO 2
			}
		}

		[MustUseReturnValue]
		public static VkPhysicalDevice[] GetPhysicalDevices(VkInstance vkInstance) {
			uint deviceCount;
			Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, null);

			if (deviceCount == 0) { return Array.Empty<VkPhysicalDevice>(); }

			VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
			fixed (VkPhysicalDevice* physicalDevicesPtr = physicalDevices) {
				Vk.EnumeratePhysicalDevices(vkInstance, &deviceCount, physicalDevicesPtr);
				return physicalDevices;
			}
		}

		[MustUseReturnValue]
		public static VkExtensionProperties[] GetInstanceExtensionProperties() {
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
		public static VkBuffer CreateVertexBuffer(VkDevice vkLogicalDevice, ulong size, uint[]? queueFamilyIndices = null) =>
				CreateBuffer(vkLogicalDevice, size, VkBufferUsageFlagBits.BufferUsageVertexBufferBit, queueFamilyIndices);

		[MustUseReturnValue]
		public static VkBuffer CreateBuffer(VkDevice vkLogicalDevice, ulong size, VkBufferUsageFlagBits vkBufferUsage, uint[]? queueFamilyIndices = null) {
			if (queueFamilyIndices == null) { return CreateBuffer(vkLogicalDevice, new() { size = size, usage = vkBufferUsage, sharingMode = VkSharingMode.SharingModeExclusive, }); }

			fixed (uint* queueFamilyIndicesPtr = queueFamilyIndices) {
				return CreateBuffer(vkLogicalDevice,
					new() { size = size, usage = vkBufferUsage, sharingMode = VkSharingMode.SharingModeConcurrent, queueFamilyIndexCount = (uint)queueFamilyIndices.Length, pQueueFamilyIndices = queueFamilyIndicesPtr, });
			}
		}

		[MustUseReturnValue]
		public static VkDeviceMemory CreateDeviceMemory(VkPhysicalDevice vkPhysicalDevice, VkDevice vkLogicalDevice, VkBuffer vkBuffer, VkMemoryPropertyFlagBits vkMemoryPropertyFlag) {
			VkMemoryRequirements vkMemoryRequirements = new(); // TODO 2
			Vk.GetBufferMemoryRequirements(vkLogicalDevice, vkBuffer, &vkMemoryRequirements);

			VkMemoryAllocateInfo vkMemoryAllocateInfo = new() { allocationSize = vkMemoryRequirements.size, memoryTypeIndex = FindMemoryType(vkPhysicalDevice, vkMemoryRequirements.memoryTypeBits, vkMemoryPropertyFlag), };
			VkDeviceMemory vkVertexBufferMemory;

			// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer."
			// "The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation among many different objects by using the offset parameters that we've seen in many functions."
			return Vk.AllocateMemory(vkLogicalDevice, &vkMemoryAllocateInfo, null, &vkVertexBufferMemory) != VkResult.Success ? throw new VulkanException("Failed to allocate memory") : vkVertexBufferMemory;
		}

		public static void CreateBufferAndMemory(VkPhysicalDevice vkPhysicalDevice, VkDevice vkLogicalDevice, ulong size, VkBufferUsageFlagBits vkBufferUsage, VkMemoryPropertyFlagBits vkMemoryPropertyFlag, out VkBuffer vkBuffer,
			out VkDeviceMemory vkDeviceMemory) {
			vkBuffer = CreateBuffer(vkLogicalDevice, size, vkBufferUsage);
			vkDeviceMemory = CreateDeviceMemory(vkPhysicalDevice, vkLogicalDevice, vkBuffer, vkMemoryPropertyFlag);
			Vk.BindBufferMemory(vkLogicalDevice, vkBuffer, vkDeviceMemory, 0);
		}

		public static void MapMemory(VkDevice vkLogicalDevice, VkDeviceMemory vkDeviceMemory, TestVertex[] vertices) {
			ulong bufferSize = (ulong)(sizeof(TestVertex) * vertices.Length);

			fixed (TestVertex* verticesPtr = vertices) {
				void* data;
				Vk.MapMemory(vkLogicalDevice, vkDeviceMemory, 0, bufferSize, 0, &data); // TODO 2
				Buffer.MemoryCopy(verticesPtr, data, bufferSize, bufferSize);
				Vk.UnmapMemory(vkLogicalDevice, vkDeviceMemory);
			}
		}

		public static void CopyBuffer(VkDevice vkLogicalDevice, VkQueue vkTransferQueue, VkCommandPool vkCommandPool, VkBuffer srcBuffer, VkBuffer dstBuffer, ulong bufferSize) {
			VkCommandBuffer vkCommandBuffer = CreateCommandBuffers(vkLogicalDevice, vkCommandPool, 1)[0];

			VkCommandBufferBeginInfo beginInfo = new() { flags = VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit, };
			Vk.BeginCommandBuffer(vkCommandBuffer, &beginInfo);

			VkBufferCopy vkBufferCopy = new() { size = bufferSize, }; // TODO 2
			Vk.CmdCopyBuffer(vkCommandBuffer, srcBuffer, dstBuffer, 1, &vkBufferCopy);

			Vk.EndCommandBuffer(vkCommandBuffer);

			VkSubmitInfo vkSubmitInfo = new() { commandBufferCount = 1, pCommandBuffers = &vkCommandBuffer, };
			Vk.QueueSubmit(vkTransferQueue, 1, &vkSubmitInfo, VkFence.Zero);
			Vk.QueueWaitIdle(vkTransferQueue);

			Vk.FreeCommandBuffers(vkLogicalDevice, vkCommandPool, 1, &vkCommandBuffer);
		}

		[MustUseReturnValue]
		private static VkBuffer CreateBuffer(VkDevice vkLogicalDevice, VkBufferCreateInfo vkBufferCreateInfo) {
			VkBuffer vkBuffer;
			return Vk.CreateBuffer(vkLogicalDevice, &vkBufferCreateInfo, null, &vkBuffer) != VkResult.Success ? throw new VulkanException("Failed to create buffer") : vkBuffer;
		}

		[MustUseReturnValue]
		private static uint FindMemoryType(VkPhysicalDevice vkPhysicalDevice, uint typeFilter, VkMemoryPropertyFlagBits vkMemoryProperty) {
			VkPhysicalDeviceMemoryProperties vkMemoryProperties = new(); // TODO 2
			Vk.GetPhysicalDeviceMemoryProperties(vkPhysicalDevice, &vkMemoryProperties);

			for (uint i = 0; i < vkMemoryProperties.memoryTypeCount; i++) {
				if ((uint)(typeFilter & (1 << (int)i)) != 0 && (vkMemoryProperties.memoryTypes[(int)i].propertyFlags & vkMemoryProperty) == vkMemoryProperty) { return i; }
			}

			throw new VulkanException("Failed to find suitable memory type");
		}

		[MustUseReturnValue]
		private static SwapChainSupportInfo QuerySwapChainSupport(VkPhysicalDevice vkPhysicalDevice, VkSurfaceKHR vkSurface) { // turns out this changes so it shouldn't be stored
			VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo = new() { surface = vkSurface, };
			VkSurfaceCapabilities2KHR surfaceCapabilities = new();

			Vk.GetPhysicalDeviceSurfaceCapabilities2KHR(vkPhysicalDevice, &surfaceInfo, &surfaceCapabilities);

			VkSurfaceFormat2KHR[] surfaceFormats = GetPhysicalDeviceSurfaceFormats(vkPhysicalDevice, surfaceInfo);
			if (surfaceFormats.Length == 0) { throw new Engine3Exception("No surface formats found"); }

			VkPresentModeKHR[] presentModes = GetPhysicalDeviceSurfacePresentModes(vkPhysicalDevice, vkSurface);
			return presentModes.Length == 0 ? throw new Engine3Exception("No present modes found") : new(surfaceCapabilities, surfaceFormats, presentModes);

			[MustUseReturnValue]
			static VkSurfaceFormat2KHR[] GetPhysicalDeviceSurfaceFormats(VkPhysicalDevice vkPhysicalDevice, VkPhysicalDeviceSurfaceInfo2KHR vkSurfaceInfo) {
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
			static VkPresentModeKHR[] GetPhysicalDeviceSurfacePresentModes(VkPhysicalDevice vkPhysicalDevice, VkSurfaceKHR vkSurface) {
				uint presentModeCount;
				Vk.GetPhysicalDeviceSurfacePresentModesKHR(vkPhysicalDevice, vkSurface, &presentModeCount, null);
				if (presentModeCount == 0) { return Array.Empty<VkPresentModeKHR>(); }

				VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
				fixed (VkPresentModeKHR* presentModesPtr = presentModes) {
					Vk.GetPhysicalDeviceSurfacePresentModesKHR(vkPhysicalDevice, vkSurface, &presentModeCount, presentModesPtr);
					return presentModes;
				}
			}
		}

		public delegate bool IsPhysicalDeviceSuitable(VkPhysicalDeviceProperties2 vkPhysicalDeviceProperties2, VkPhysicalDeviceFeatures2 vkPhysicalDeviceFeatures2);
		public delegate int RateGpuSuitability(PhysicalGpu physicalGpu);

		private readonly record struct SwapChainSupportInfo {
			public required VkSurfaceCapabilities2KHR VkCapabilities { get; init; }
			public required VkSurfaceFormat2KHR[] VkSurfaceFormats { get; init; }
			public required VkPresentModeKHR[] VkPresentModes { get; init; }

			[SetsRequiredMembers]
			public SwapChainSupportInfo(VkSurfaceCapabilities2KHR vkCapabilities, VkSurfaceFormat2KHR[] vkSurfaceFormats, VkPresentModeKHR[] vkPresentModes) {
				VkCapabilities = vkCapabilities;
				VkSurfaceFormats = vkSurfaceFormats;
				VkPresentModes = vkPresentModes;
			}
		}
	}
}