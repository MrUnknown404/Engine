using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Graphics.Test;
using Engine3.Utils;
using JetBrains.Annotations;
using NLog;
using OpenTK.Core.Native;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using OpenTK.Platform;
using Silk.NET.Shaderc;
using Compiler = Silk.NET.Shaderc.Compiler;
using ShaderKind = Silk.NET.Shaderc.ShaderKind;
using SourceLanguage = Silk.NET.Shaderc.SourceLanguage;

namespace Engine3.Graphics.Vulkan {
	public static unsafe partial class VkH { // TODO any VK method that can take an array should
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
			string[] requiredInstanceExtensions = gameClient.GetAllRequiredInstanceExtensions();
			string[] requiredValidationLayers = gameClient.GetAllRequiredValidationLayers();

			VkApplicationInfo applicationInfo = new() {
					pApplicationName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(gameClient.Name))),
					applicationVersion = gameVersion.Packed,
					pEngineName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(engineName))),
					engineVersion = engineVersion.Packed,
					apiVersion = Vk.MAKE_API_VERSION(0, 1, 4, 0),
			};

			IntPtr requiredExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredInstanceExtensions);
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredValidationLayers);

			VkDebugUtilsMessengerCreateInfoEXT messengerCreateInfo = CreateDebugUtilsMessengerCreateInfoEXT(gameClient.EnabledDebugMessageSeverities, gameClient.EnabledDebugMessageTypes);
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
			if (Vk.CreateInstance(&instanceCreateInfo, null, &vkInstance) != VkResult.Success) { throw new VulkanException("Failed to create Vulkan instance"); }

			MarshalTk.FreeStringArrayCoTaskMem(requiredExtensionsPtr, requiredInstanceExtensions.Length);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

			return vkInstance;
		}

		[MustUseReturnValue]
		public static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) =>
				Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR surface) != VkResult.Success ? throw new VulkanException("Failed to create surface") : surface;

		[MustUseReturnValue]
		public static VkDevice CreateLogicalDevice(VkPhysicalDevice physicalDevice, QueueFamilyIndices queueFamilyIndices, string[] requiredDeviceExtensions, string[] requiredValidationLayers) {
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

				if (Vk.CreateDevice(physicalDevice, &deviceCreateInfo, null, &logicalDevice) != VkResult.Success) { throw new VulkanException("Failed to create logical device"); }
			}

			MarshalTk.FreeStringArrayCoTaskMem(requiredDeviceExtensionsPtr, requiredDeviceExtensions.Length);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

			return logicalDevice;
		}

		[MustUseReturnValue]
		public static PhysicalGpu[] GetValidGpus(VkPhysicalDevice[] physicalDevices, VkSurfaceKHR surface, IsPhysicalDeviceSuitable isPhysicalDeviceSuitable, string[] requiredDeviceExtensions) {
			List<PhysicalGpu> gpus = new();

			foreach (VkPhysicalDevice physicalDevice in physicalDevices) {
				VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new();
				VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new();
				Vk.GetPhysicalDeviceProperties2(physicalDevice, &physicalDeviceProperties2);
				Vk.GetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

				if (!isPhysicalDeviceSuitable(physicalDeviceProperties2, physicalDeviceFeatures2)) { continue; }

				VkExtensionProperties[] physicalDeviceExtensionProperties = GetPhysicalDeviceExtensionProperties(physicalDevice);
				if (physicalDeviceExtensionProperties.Length == 0) { throw new VulkanException("Could not find any device extension properties"); }
				if (!CheckDeviceExtensionSupport(physicalDeviceExtensionProperties, requiredDeviceExtensions)) { continue; }
				if (!FindQueueFamilies(physicalDevice, surface, out uint? graphicsFamily, out uint? presentFamily, out uint? transferFamily)) { continue; }
				if (!QuerySwapChainSupport(physicalDevice, surface, out _, out _, out _)) { continue; }

				gpus.Add(new(physicalDevice, physicalDeviceProperties2, physicalDeviceFeatures2, physicalDeviceExtensionProperties, new(graphicsFamily.Value, presentFamily.Value, transferFamily.Value)));
			}

			return gpus.ToArray();

			// idk. not sure if i like local methods

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
			static VkQueueFamilyProperties2[] GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice) {
				uint queueFamilyPropertyCount = 0;
				Vk.GetPhysicalDeviceQueueFamilyProperties2(physicalDevice, &queueFamilyPropertyCount, null);

				if (queueFamilyPropertyCount == 0) { return Array.Empty<VkQueueFamilyProperties2>(); }

				VkQueueFamilyProperties2[] queueFamilyProperties2 = new VkQueueFamilyProperties2[queueFamilyPropertyCount];
				for (int i = 0; i < queueFamilyPropertyCount; i++) { queueFamilyProperties2[i] = new() { sType = VkStructureType.StructureTypeQueueFamilyProperties2, }; }

				fixed (VkQueueFamilyProperties2* queueFamilyPropertiesPtr = queueFamilyProperties2) {
					Vk.GetPhysicalDeviceQueueFamilyProperties2(physicalDevice, &queueFamilyPropertyCount, queueFamilyPropertiesPtr);
					return queueFamilyProperties2;
				}
			}

			[MustUseReturnValue]
			static bool CheckDeviceExtensionSupport(VkExtensionProperties[] physicalDeviceExtensionProperties, string[] requiredDeviceExtensions) =>
					requiredDeviceExtensions.All(wantedExtension => physicalDeviceExtensionProperties.Any(extensionProperties => {
						ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
						return Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension;
					}));

			[MustUseReturnValue]
			static bool FindQueueFamilies(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, [NotNullWhen(true)] out uint? graphicsFamily, [NotNullWhen(true)] out uint? presentFamily,
				[NotNullWhen(true)] out uint? transferFamily) {
				graphicsFamily = null;
				presentFamily = null;
				transferFamily = null;

				VkQueueFamilyProperties2[] queueFamilyProperties2 = GetPhysicalDeviceQueueFamilyProperties(physicalDevice);

				for (uint i = 0; i < queueFamilyProperties2.Length; i++) {
					VkQueueFamilyProperties queueFamilyProperties1 = queueFamilyProperties2[i].queueFamilyProperties;
					if ((queueFamilyProperties1.queueFlags & VkQueueFlagBits.QueueGraphicsBit) != 0) { graphicsFamily = i; }
					if ((queueFamilyProperties1.queueFlags & VkQueueFlagBits.QueueTransferBit) != 0) { transferFamily = i; }

					int presentSupport;
					Vk.GetPhysicalDeviceSurfaceSupportKHR(physicalDevice, i, surface, &presentSupport);
					if (presentSupport == Vk.True) { presentFamily = i; }

					if (graphicsFamily != null && presentFamily != null && transferFamily != null) { return true; }
				}

				return false;
			}
		}

		[MustUseReturnValue]
		public static PhysicalGpu? PickBestGpu(PhysicalGpu[] physicalGpus, RateGpuSuitability rateGpuSuitability) {
			PhysicalGpu? bestDevice = null;
			int bestDeviceScore = 0;

			foreach (PhysicalGpu device in physicalGpus) {
				int score = rateGpuSuitability(device);
				if (score > bestDeviceScore) {
					bestDevice = device;
					bestDeviceScore = score;
				}
			}

			return bestDevice;
		}

		public static void PrintGpus(PhysicalGpu[] physicalGpus, bool verbose) {
			Logger.Debug("The following GPUs are available:");
			foreach (PhysicalGpu gpu in physicalGpus) {
				if (verbose) {
					foreach (string str in gpu.GetVerboseDescription()) { Logger.Debug(str); }
				} else {
					Logger.Debug($"- {gpu.GetSimpleDescription()}"); //
				}
			}
		}

		[MustUseReturnValue]
		public static SwapChain CreateSwapChain(PhysicalGpu physicalGpu, VkDevice logicalDevice, VkSurfaceKHR surface, Vector2i framebufferSize, VkPresentModeKHR presentMode = VkPresentModeKHR.PresentModeMailboxKhr,
			VkSurfaceTransformFlagBitsKHR? vkSurfaceTransform = null, VkSwapchainKHR? oldSwapChain = null) {
			if (!QuerySwapChainSupport(physicalGpu.PhysicalDevice, surface, out VkSurfaceCapabilities2KHR surfaceCapabilities2, out VkSurfaceFormat2KHR[]? surfaceFormats2, out VkPresentModeKHR[]? supportedPresentModes)) {
				throw new VulkanException("Failed to query swap chain support");
			}

			if (!supportedPresentModes.Contains(presentMode)) { throw new VulkanException("Surface does not support requested present mode"); }

			VkSurfaceCapabilitiesKHR surfaceCapabilities = surfaceCapabilities2.surfaceCapabilities;
			QueueFamilyIndices queueFamilyIndices = physicalGpu.QueueFamilyIndices;
			VkSurfaceFormat2KHR surfaceFormat = ChooseSwapSurfaceFormat(surfaceFormats2) ?? throw new VulkanException("Could not find any valid surface formats");
			VkFormat swapChainImageFormat = surfaceFormat.surfaceFormat.format;
			VkExtent2D swapChainExtent = ChooseSwapExtent(framebufferSize, surfaceCapabilities);

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

			VkSwapchainKHR swapChain;
			fixed (uint* pQueueFamilyIndicesPtr = queueFamilyIndicesArray) {
				VkSwapchainCreateInfoKHR createInfo = new() {
						surface = surface,
						imageFormat = swapChainImageFormat,
						imageColorSpace = surfaceFormat.surfaceFormat.colorSpace,
						imageExtent = swapChainExtent,
						minImageCount = imageCount,
						imageArrayLayers = 1,
						imageUsage = VkImageUsageFlagBits.ImageUsageColorAttachmentBit,
						imageSharingMode = imageSharingMode,
						queueFamilyIndexCount = queueFamilyIndexCount,
						pQueueFamilyIndices = queueFamilyIndicesArray.Length == 0 ? null : pQueueFamilyIndicesPtr,
						preTransform = vkSurfaceTransform ?? surfaceCapabilities.currentTransform,
						compositeAlpha = VkCompositeAlphaFlagBitsKHR.CompositeAlphaOpaqueBitKhr,
						presentMode = presentMode,
						clipped = (int)Vk.True,
						oldSwapchain = oldSwapChain ?? VkSwapchainKHR.Zero,
				};

				VkResult vkResult = Vk.CreateSwapchainKHR(logicalDevice, &createInfo, null, &swapChain);
				if (vkResult != VkResult.Success) { throw new VulkanException($"Failed to create swap chain. {vkResult}"); }
			}

			VkImage[] swapChainImages = GetSwapChainImages(logicalDevice, swapChain);
			VkImageView[] swapChainImageViews = CreateImageViews(logicalDevice, swapChainImages, swapChainImageFormat);

			return new(swapChain, swapChainImageFormat, swapChainExtent, swapChainImages, swapChainImageViews, presentMode);

			[MustUseReturnValue]
			static VkSurfaceFormat2KHR? ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormat2KHR> availableFormats) =>
					availableFormats.FirstOrDefault(static format => format.surfaceFormat is { format: VkFormat.FormatB8g8r8a8Srgb, colorSpace: VkColorSpaceKHR.ColorSpaceSrgbNonlinearKhr, });

			[MustUseReturnValue]
			static VkExtent2D ChooseSwapExtent(Vector2i framebufferSize, VkSurfaceCapabilitiesKHR surfaceCapabilities) =>
					surfaceCapabilities.currentExtent.width != uint.MaxValue ?
							surfaceCapabilities.currentExtent :
							new() {
									width = Math.Clamp((uint)framebufferSize.X, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width),
									height = Math.Clamp((uint)framebufferSize.Y, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height),
							};

			[MustUseReturnValue]
			static VkImage[] GetSwapChainImages(VkDevice logicalDevice, VkSwapchainKHR swapChain) {
				uint swapChainImageCount;
				Vk.GetSwapchainImagesKHR(logicalDevice, swapChain, &swapChainImageCount, null);

				VkImage[] swapChainImages = new VkImage[swapChainImageCount];
				fixed (VkImage* swapChainImagesPtr = swapChainImages) {
					Vk.GetSwapchainImagesKHR(logicalDevice, swapChain, &swapChainImageCount, swapChainImagesPtr);
					return swapChainImages;
				}
			}

			[MustUseReturnValue]
			static VkImageView[] CreateImageViews(VkDevice logicalDevice, VkImage[] swapChainImages, VkFormat swapChainFormat) {
				VkImageView[] imageViews = new VkImageView[swapChainImages.Length];

				fixed (VkImageView* imageViewsPtr = imageViews) {
					// ReSharper disable once LoopCanBeConvertedToQuery // nope
					for (int i = 0; i < swapChainImages.Length; i++) {
						VkImageViewCreateInfo createInfo = new() {
								image = swapChainImages[i],
								viewType = VkImageViewType.ImageViewType2d,
								format = swapChainFormat,
								components = new() {
										r = VkComponentSwizzle.ComponentSwizzleIdentity,
										g = VkComponentSwizzle.ComponentSwizzleIdentity,
										b = VkComponentSwizzle.ComponentSwizzleIdentity,
										a = VkComponentSwizzle.ComponentSwizzleIdentity,
								},
								subresourceRange = new() { aspectMask = VkImageAspectFlagBits.ImageAspectColorBit, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1, },
						};

						if (Vk.CreateImageView(logicalDevice, &createInfo, null, &imageViewsPtr[i]) != VkResult.Success) { throw new VulkanException("Failed to create image views"); }
					}
				}

				return imageViews;
			}
		}

		[MustUseReturnValue]
		public static bool CheckSupportForRequiredInstanceExtensions(VkExtensionProperties[] instanceExtensionProperties, string[] requiredInstanceExtensions) =>
				requiredInstanceExtensions.All(wantedExtension => instanceExtensionProperties.Any(extensionProperties => {
					ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
					return Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension;
				}));

		// TODO maybe make a GraphicsPipelineBuilder? idk.
		public static void CreateGraphicsPipeline(VkDevice logicalDevice, VkFormat swapChainImageFormat, VkPipelineShaderStageCreateInfo[] shaderStageCreateInfos, VkDescriptorSetLayout[]? descriptorSetLayouts,
			out VkPipeline graphicsPipeline, out VkPipelineLayout pipelineLayout) {
			if (shaderStageCreateInfos.Length == 0) { throw new VulkanException($"{nameof(shaderStageCreateInfos)} cannot be empty"); }

			VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new() { topology = VkPrimitiveTopology.PrimitiveTopologyTriangleList, };
			VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new() { viewportCount = 1, scissorCount = 1, };
			VkPipelineRenderingCreateInfo renderingCreateInfo = new() { colorAttachmentCount = 1, pColorAttachmentFormats = &swapChainImageFormat, };

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

			fixed (VkDescriptorSetLayout* descriptorSetLayoutsPtr = descriptorSetLayouts) {
				VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new() {
						setLayoutCount = descriptorSetLayouts != null ? (uint)descriptorSetLayouts.Length : 0,
						pSetLayouts = descriptorSetLayouts != null ? descriptorSetLayoutsPtr : null,
						pushConstantRangeCount = 0,
						pPushConstantRanges = null,
				};

				VkPipelineLayout tempPipelineLayout;
				if (Vk.CreatePipelineLayout(logicalDevice, &pipelineLayoutCreateInfo, null, &tempPipelineLayout) != VkResult.Success) { throw new VulkanException("Failed to create pipeline layout"); }
				pipelineLayout = tempPipelineLayout;
			}

			VkDynamicState[] dynamicStates = [ VkDynamicState.DynamicStateViewport, VkDynamicState.DynamicStateScissor, ]; // TODO allow this to be edited
			VkVertexInputAttributeDescription[] attributeDescriptions = TestVertex.GetAttributeDescriptions();

			fixed (VkPipelineShaderStageCreateInfo* shaderStageCreateInfosPtr = shaderStageCreateInfos) {
				fixed (VkDynamicState* dynamicStatesPtr = dynamicStates) {
					fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions) {
						VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new() { dynamicStateCount = (uint)dynamicStates.Length, pDynamicStates = dynamicStatesPtr, };
						VkVertexInputBindingDescription bindingDescription = TestVertex.GetBindingDescription();

						VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new() {
								vertexBindingDescriptionCount = 1,
								vertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
								pVertexBindingDescriptions = &bindingDescription,
								pVertexAttributeDescriptions = attributeDescriptionsPtr,
						};

						VkGraphicsPipelineCreateInfo pipelineCreateInfo = new() {
								pNext = &renderingCreateInfo,
								stageCount = (uint)shaderStageCreateInfos.Length,
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

						VkPipeline tempGraphicsPipeline;
						if (Vk.CreateGraphicsPipelines(logicalDevice, VkPipelineCache.Zero, 1, &pipelineCreateInfo, null, &tempGraphicsPipeline) != VkResult.Success) {
							throw new VulkanException("Failed to create graphics pipeline");
						}

						graphicsPipeline = tempGraphicsPipeline;
					}
				}
			}
		}

		[MustUseReturnValue]
		public static VkQueue GetDeviceQueue(VkDevice logicalDevice, uint queueFamilyIndex) {
			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
			VkQueue queue;
			Vk.GetDeviceQueue2(logicalDevice, &deviceQueueInfo2, &queue);
			return queue;
		}

		[MustUseReturnValue]
		public static VkShaderModule? CreateShaderModule(VkDevice logicalDevice, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) {
			string fullFileName = $"{fileLocation}.{shaderType.FileExtension}.{shaderLang.FileExtension}";

			using Stream? shaderStream = AssetH.GetAssetStream($"Shaders.{fullFileName}", assembly);
			if (shaderStream == null) {
				Logger.Error("Failed to create asset stream");
				return null;
			}

			switch (shaderLang) {
				case ShaderLanguage.Glsl or ShaderLanguage.Hlsl: {
					Shaderc shaderc = Engine3.GameInstance.Shaderc;

					Compiler* compiler = shaderc.CompilerInitialize();
					CompileOptions* options = shaderc.CompileOptionsInitialize();

					shaderc.CompileOptionsSetSourceLanguage(options, shaderLang switch {
							ShaderLanguage.Glsl => SourceLanguage.Glsl,
							ShaderLanguage.Hlsl => SourceLanguage.Hlsl,
							ShaderLanguage.SpirV => throw new UnreachableException(),
							_ => throw new NotImplementedException(),
					});

					using StreamReader streamReader = new(shaderStream);
					string source = streamReader.ReadToEnd();
					byte* sourcePtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(source)));
					byte* shaderNamePtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes(fullFileName)));
					ShaderKind shaderKind = shaderType switch {
							ShaderType.Fragment => ShaderKind.FragmentShader,
							ShaderType.Vertex => ShaderKind.VertexShader,
							ShaderType.Geometry => ShaderKind.GeometryShader,
							ShaderType.TessEvaluation => ShaderKind.TessEvaluationShader,
							ShaderType.TessControl => ShaderKind.TessControlShader,
							ShaderType.Compute => ShaderKind.ComputeShader,
							_ => throw new ArgumentOutOfRangeException(nameof(shaderType), shaderType, null),
					};

					CompilationResult* result = shaderc.CompileIntoSpv(compiler, sourcePtr, (nuint)source.Length, shaderKind, shaderNamePtr, "main", options);
					shaderc.CompileOptionsRelease(options);

					CompilationStatus status = shaderc.ResultGetCompilationStatus(result);
					shaderc.CompilerRelease(compiler);

					if (status != CompilationStatus.Success) {
						Logger.Error($"Failed to compile {shaderType} shader: {fileLocation}. {shaderc.ResultGetErrorMessageS(result)}");
						shaderc.ResultRelease(result);
						return null;
					}

					VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = shaderc.ResultGetLength(result), pCode = (uint*)shaderc.ResultGetBytes(result), };
					VkShaderModule shaderModule;

					if (Vk.CreateShaderModule(logicalDevice, &shaderModuleCreateInfo, null, &shaderModule) != VkResult.Success) {
						shaderc.ResultRelease(result);
						throw new VulkanException("Failed to create shader module");
					}

					shaderc.ResultRelease(result);

					return shaderModule;
				}
				case ShaderLanguage.SpirV: {
					using BinaryReader reader = new(shaderStream);
					byte[] data = reader.ReadBytes((int)shaderStream.Length);

					fixed (byte* shaderCodePtr = data) {
						VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = (UIntPtr)data.Length, pCode = (uint*)shaderCodePtr, };
						VkShaderModule shaderModule;
						return Vk.CreateShaderModule(logicalDevice, &shaderModuleCreateInfo, null, &shaderModule) != VkResult.Success ? throw new VulkanException("Failed to create shader module") : shaderModule;
					}
				}
				default: throw new ArgumentOutOfRangeException(nameof(shaderLang), shaderLang, null);
			}
		}

		[MustUseReturnValue]
		public static VkCommandPool CreateCommandPool(VkDevice logicalDevice, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			return Vk.CreateCommandPool(logicalDevice, &commandPoolCreateInfo, null, &commandPool) != VkResult.Success ? throw new VulkanException("Failed to create command pool") : commandPool;
		}

		[MustUseReturnValue]
		public static VkCommandBuffer[] CreateCommandBuffers(VkDevice logicalDevice, VkCommandPool commandPool, uint count, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = count, };
			VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];
			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				return Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, commandBuffersPtr) != VkResult.Success ? throw new VulkanException("Failed to create command buffer") : commandBuffers;
			}
		}

		[MustUseReturnValue]
		public static VkCommandBuffer CreateCommandBuffer(VkDevice logicalDevice, VkCommandPool commandPool, uint count, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = count, };
			VkCommandBuffer commandBuffers;
			return Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, &commandBuffers) != VkResult.Success ? throw new VulkanException("Failed to create command buffer") : commandBuffers;
		}

		[MustUseReturnValue]
		public static VkSemaphore[] CreateSemaphores(VkDevice logicalDevice, uint count) {
			VkSemaphore[] semaphores = new VkSemaphore[count];
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();

			fixed (VkSemaphore* semaphoresPtr = semaphores) {
				for (int i = 0; i < count; i++) {
					if (Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]) != VkResult.Success) { throw new VulkanException("failed to create semaphore"); }
				}
			}

			return semaphores;
		}

		[MustUseReturnValue]
		public static VkSemaphore CreateSemaphore(VkDevice logicalDevice) {
			VkSemaphore semaphore;
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			return Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphore) != VkResult.Success ? throw new VulkanException("failed to create semaphore") : semaphore;
		}

		[MustUseReturnValue]
		public static VkFence[] CreateFences(VkDevice logicalDevice, uint count) {
			VkFence[] fences = new VkFence[count];
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };

			fixed (VkFence* fencesPtr = fences) {
				for (int i = 0; i < count; i++) {
					if (Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fencesPtr[i]) != VkResult.Success) { throw new VulkanException("Failed to create fence"); }
				}
			}

			return fences;
		}

		[MustUseReturnValue]
		public static VkFence CreateFence(VkDevice logicalDevice) {
			VkFence fence;
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			return Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fence) != VkResult.Success ? throw new VulkanException("Failed to create fence") : fence;
		}

		public static void SubmitCommandBuffersQueue(VkQueue queue, VkCommandBuffer[] commandBuffers, VkSemaphore waitSemaphore, VkSemaphore signalSemaphore, VkFence inFlight) {
			VkPipelineStageFlagBits[] waitStages = [ VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, ];

			fixed (VkPipelineStageFlagBits* waitStagesPtr = waitStages) {
				fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
					VkSubmitInfo submitInfo = new() { // TODO 2
							waitSemaphoreCount = 1,
							pWaitSemaphores = &waitSemaphore,
							pWaitDstStageMask = waitStagesPtr,
							commandBufferCount = (uint)commandBuffers.Length,
							pCommandBuffers = commandBuffersPtr,
							signalSemaphoreCount = 1,
							pSignalSemaphores = &signalSemaphore,
					};

					SubmitQueue(queue, [ submitInfo, ], inFlight);
				}
			}
		}

		public static void SubmitCommandBufferQueue(VkQueue queue, VkCommandBuffer commandBuffer, VkSemaphore waitSemaphore, VkSemaphore signalSemaphore, VkFence inFlight) {
			VkPipelineStageFlagBits[] waitStages = [ VkPipelineStageFlagBits.PipelineStageColorAttachmentOutputBit, ];

			fixed (VkPipelineStageFlagBits* waitStagesPtr = waitStages) {
				VkSubmitInfo submitInfo = new() { // TODO 2
						waitSemaphoreCount = 1,
						pWaitSemaphores = &waitSemaphore,
						pWaitDstStageMask = waitStagesPtr,
						commandBufferCount = 1,
						pCommandBuffers = &commandBuffer,
						signalSemaphoreCount = 1,
						pSignalSemaphores = &signalSemaphore,
				};

				SubmitQueue(queue, submitInfo, inFlight);
			}
		}

		public static void SubmitQueue(VkQueue queue, VkSubmitInfo submitInfo, VkFence fence) {
			if (Vk.QueueSubmit(queue, 1, &submitInfo, fence) != VkResult.Success) { throw new VulkanException("Failed to submit queue"); } // TODO 2
		}

		public static void SubmitQueue(VkQueue queue, VkSubmitInfo[] submitInfos, VkFence fence) {
			fixed (VkSubmitInfo* submitInfosPtr = submitInfos) {
				if (Vk.QueueSubmit(queue, (uint)submitInfos.Length, submitInfosPtr, fence) != VkResult.Success) { throw new VulkanException("Failed to submit queue"); } // TODO 2
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
		public static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsage, ulong size, uint[]? queueFamilyIndices = null) {
			if (queueFamilyIndices == null) { return CreateBuffer(logicalDevice, new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeExclusive, }); }

			fixed (uint* queueFamilyIndicesPtr = queueFamilyIndices) {
				return CreateBuffer(logicalDevice,
					new() { size = size, usage = bufferUsage, sharingMode = VkSharingMode.SharingModeConcurrent, queueFamilyIndexCount = (uint)queueFamilyIndices.Length, pQueueFamilyIndices = queueFamilyIndicesPtr, });
			}
		}

		[MustUseReturnValue]
		public static VkDeviceMemory CreateDeviceMemory(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBuffer buffer, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkMemoryRequirements memoryRequirements = new(); // TODO 2
			Vk.GetBufferMemoryRequirements(logicalDevice, buffer, &memoryRequirements);

			VkMemoryAllocateInfo memoryAllocateInfo = new() { allocationSize = memoryRequirements.size, memoryTypeIndex = FindMemoryType(physicalDevice, memoryRequirements.memoryTypeBits, memoryPropertyFlags), };
			VkDeviceMemory deviceMemory;

			// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer."
			// "The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation among many different objects by using the offset parameters that we've seen in many functions."
			return Vk.AllocateMemory(logicalDevice, &memoryAllocateInfo, null, &deviceMemory) != VkResult.Success ? throw new VulkanException("Failed to allocate memory") : deviceMemory;
		}

		public static void CreateBufferAndMemory(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBufferUsageFlagBits bufferUsage, VkMemoryPropertyFlagBits memoryPropertyFlags, ulong size, out VkBuffer buffer,
			out VkDeviceMemory deviceMemory) {
			buffer = CreateBuffer(logicalDevice, bufferUsage, size);
			deviceMemory = CreateDeviceMemory(physicalDevice, logicalDevice, buffer, memoryPropertyFlags);
			Vk.BindBufferMemory(logicalDevice, buffer, deviceMemory, 0);
		}

		public static void MapMemory<T>(VkDevice logicalDevice, VkDeviceMemory deviceMemory, T[] inData) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * inData.Length);
			fixed (T* inDataPtr = inData) {
				void* data;
				Vk.MapMemory(logicalDevice, deviceMemory, 0, bufferSize, 0, &data); // TODO 2
				Buffer.MemoryCopy(inDataPtr, data, bufferSize, bufferSize);
				Vk.UnmapMemory(logicalDevice, deviceMemory);
			}
		}

		public static void CopyBuffer(VkDevice logicalDevice, VkQueue transferQueue, VkCommandPool transferCommandPool, VkBuffer srcBuffer, VkBuffer dstBuffer, ulong bufferSize) {
			VkCommandBuffer commandBuffer = CreateCommandBuffer(logicalDevice, transferCommandPool, 1);

			VkCommandBufferBeginInfo beginInfo = new() { flags = VkCommandBufferUsageFlagBits.CommandBufferUsageOneTimeSubmitBit, };
			Vk.BeginCommandBuffer(commandBuffer, &beginInfo);

			VkBufferCopy bufferCopy = new() { size = bufferSize, }; // TODO 2
			Vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &bufferCopy);

			Vk.EndCommandBuffer(commandBuffer);

			VkSubmitInfo submitInfo = new() { commandBufferCount = 1, pCommandBuffers = &commandBuffer, };
			SubmitQueue(transferQueue, [ submitInfo, ], VkFence.Zero);
			Vk.QueueWaitIdle(transferQueue);

			Vk.FreeCommandBuffers(logicalDevice, transferCommandPool, 1, &commandBuffer);
		}

		public static void CreateDescriptorSets(VkDevice logicalDevice, VkDescriptorPool descriptorPool, VkDescriptorSetLayout descriptorSetLayout, VkDescriptorSet[] descriptorSets, uint maxFramesInFlight, uint uniformBufferSize,
			VkBuffer[] uniformBuffers) {
			VkDescriptorSetLayout[] layouts = new VkDescriptorSetLayout[maxFramesInFlight];
			for (int i = 0; i < maxFramesInFlight; i++) { layouts[i] = descriptorSetLayout; }

			fixed (VkDescriptorSetLayout* layoutsPtr = layouts) {
				fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
					VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = descriptorPool, descriptorSetCount = maxFramesInFlight, pSetLayouts = layoutsPtr, };
					if (Vk.AllocateDescriptorSets(logicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr) != VkResult.Success) { throw new VulkanException("Failed to allocation descriptor sets"); }
				}
			}

			for (int i = 0; i < maxFramesInFlight; i++) {
				VkDescriptorBufferInfo descriptorBufferInfo = new() { buffer = uniformBuffers[i], offset = 0, range = uniformBufferSize, };
				VkWriteDescriptorSet writeDescriptorSet = new() {
						dstSet = descriptorSets[i],
						dstBinding = 0,
						dstArrayElement = 0,
						descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer,
						descriptorCount = 1,
						pBufferInfo = &descriptorBufferInfo,
						pImageInfo = null,
						pTexelBufferView = null,
				};

				Vk.UpdateDescriptorSets(logicalDevice, 1, &writeDescriptorSet, 0, null);
			}
		}

		[MustUseReturnValue]
		public static VkDescriptorSetLayout CreateDescriptorSetLayout(VkDevice logicalDevice, uint binding, VkShaderStageFlagBits shaderStageFlags) {
			VkDescriptorSetLayoutBinding uboLayoutBinding = new() { binding = binding, descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, stageFlags = shaderStageFlags, };
			VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = new() { bindingCount = 1, pBindings = &uboLayoutBinding, };
			VkDescriptorSetLayout layout;
			return Vk.CreateDescriptorSetLayout(logicalDevice, &descriptorSetLayoutCreateInfo, null, &layout) != VkResult.Success ? throw new VulkanException("Failed to create descriptor set layout") : layout;
		}

		[MustUseReturnValue]
		public static VkDescriptorPool CreateDescriptorPool(VkDevice logicalDevice, uint maxFramesInFlight) {
			VkDescriptorPoolSize descriptorPoolSize = new() { descriptorCount = maxFramesInFlight, };
			VkDescriptorPoolCreateInfo descriptorPoolCreateInfo = new() { poolSizeCount = 1, pPoolSizes = &descriptorPoolSize, maxSets = maxFramesInFlight, };
			VkDescriptorPool descriptorPool;
			return Vk.CreateDescriptorPool(logicalDevice, &descriptorPoolCreateInfo, null, &descriptorPool) != VkResult.Success ? throw new VulkanException("Failed to create descriptor pool") : descriptorPool;
		}

		[MustUseReturnValue]
		public static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferCreateInfo bufferCreateInfo) {
			VkBuffer buffer;
			return Vk.CreateBuffer(logicalDevice, &bufferCreateInfo, null, &buffer) != VkResult.Success ? throw new VulkanException("Failed to create buffer") : buffer;
		}

		/// <summary> Creates a <see cref="VkBuffer"/> &amp; <see cref="VkDeviceMemory"/>. Then copies vertex data using into our new buffer using a staging buffer </summary>
		public static void CreateBufferUsingStagingBuffer<T>(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferPool, VkQueue transferQueue, T[] bufferData, VkBufferUsageFlagBits bufferUsage,
			out VkBuffer buffer, out VkDeviceMemory bufferMemory) where T : unmanaged {
			ulong bufferSize = (ulong)(sizeof(T) * bufferData.Length);

			CreateBufferAndMemory(physicalDevice, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferSrcBit, VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit,
				bufferSize, out VkBuffer stagingBuffer, out VkDeviceMemory stagingBufferMemory); // TODO should i make a persistent staging buffer?

			MapMemory(logicalDevice, stagingBufferMemory, bufferData);

			CreateBufferAndMemory(physicalDevice, logicalDevice, VkBufferUsageFlagBits.BufferUsageTransferDstBit | bufferUsage, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, bufferSize, out buffer, out bufferMemory);

			CopyBuffer(logicalDevice, transferQueue, transferPool, stagingBuffer, buffer, bufferSize);

			Vk.DestroyBuffer(logicalDevice, stagingBuffer, null);
			Vk.FreeMemory(logicalDevice, stagingBufferMemory, null);
		}

		public static void CmdSetViewport(VkCommandBuffer graphicsCommandBuffer, uint x, uint y, uint width, uint height, float minDepth, float maxDepth) {
			VkViewport viewport = new() { x = x, y = y, width = width, height = height, minDepth = minDepth, maxDepth = maxDepth, };
			Vk.CmdSetViewport(graphicsCommandBuffer, 0, 1, &viewport);
		}

		public static void CmdSetScissor(VkCommandBuffer graphicsCommandBuffer, VkExtent2D extent, VkOffset2D offset) {
			VkRect2D scissor = new() { offset = new(0, 0), extent = extent, };
			Vk.CmdSetScissor(graphicsCommandBuffer, 0, 1, &scissor);
		}

		public static void CmdBindVertexBuffer(VkCommandBuffer graphicsCommandBuffer, VkBuffer vertexBuffer, ulong offset) => Vk.CmdBindVertexBuffers(graphicsCommandBuffer, 0, 1, &vertexBuffer, &offset); // TODO 2

		public static void CmdBindVertexBuffers(VkCommandBuffer graphicsCommandBuffer, VkBuffer[] vertexBuffers, ulong[] offsets) {
			if (vertexBuffers.Length != offsets.Length) { throw new VulkanException("Failed to bind vertex buffers"); } // maybe a bit dramatic. maybe return bool?

			fixed (VkBuffer* vertexBuffersPtr = vertexBuffers) {
				fixed (ulong* offsetsPtr = offsets) {
					Vk.CmdBindVertexBuffers(graphicsCommandBuffer, 0, (uint)vertexBuffers.Length, vertexBuffersPtr, offsetsPtr); // TODO 2
				}
			}
		}

		[MustUseReturnValue]
		private static uint FindMemoryType(VkPhysicalDevice physicalDevice, uint typeFilter, VkMemoryPropertyFlagBits memoryPropertyFlag) {
			VkPhysicalDeviceMemoryProperties memoryProperties = new(); // TODO 2
			Vk.GetPhysicalDeviceMemoryProperties(physicalDevice, &memoryProperties);

			for (uint i = 0; i < memoryProperties.memoryTypeCount; i++) {
				if ((uint)(typeFilter & (1 << (int)i)) != 0 && (memoryProperties.memoryTypes[(int)i].propertyFlags & memoryPropertyFlag) == memoryPropertyFlag) { return i; }
			}

			throw new VulkanException("Failed to find suitable memory type");
		}

		[MustUseReturnValue]
		private static bool QuerySwapChainSupport(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, out VkSurfaceCapabilities2KHR surfaceCapabilities2, [NotNullWhen(true)] out VkSurfaceFormat2KHR[]? surfaceFormats2,
			[NotNullWhen(true)] out VkPresentModeKHR[]? presentModes) {
			surfaceFormats2 = null;
			presentModes = null;

			VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo = new() { surface = surface, };
			VkSurfaceCapabilities2KHR tempSurfaceCapabilities2 = new();
			Vk.GetPhysicalDeviceSurfaceCapabilities2KHR(physicalDevice, &surfaceInfo, &tempSurfaceCapabilities2);
			surfaceCapabilities2 = tempSurfaceCapabilities2;

			VkSurfaceFormat2KHR[] tempSurfaceFormats2 = GetPhysicalDeviceSurfaceFormats(physicalDevice, surfaceInfo);
			if (tempSurfaceFormats2.Length == 0) { return false; }
			surfaceFormats2 = tempSurfaceFormats2;

			VkPresentModeKHR[] tempPresentModes = GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface);
			if (tempPresentModes.Length == 0) { return false; }
			presentModes = tempPresentModes;

			return true;

			[MustUseReturnValue]
			static VkSurfaceFormat2KHR[] GetPhysicalDeviceSurfaceFormats(VkPhysicalDevice physicalDevice, VkPhysicalDeviceSurfaceInfo2KHR surfaceInfo2) {
				uint formatCount;
				Vk.GetPhysicalDeviceSurfaceFormats2KHR(physicalDevice, &surfaceInfo2, &formatCount, null);
				if (formatCount == 0) { return Array.Empty<VkSurfaceFormat2KHR>(); }

				VkSurfaceFormat2KHR[] surfaceFormats = new VkSurfaceFormat2KHR[formatCount];
				for (int i = 0; i < formatCount; i++) { surfaceFormats[i] = new(); }

				fixed (VkSurfaceFormat2KHR* surfaceFormatsPtr = surfaceFormats) {
					Vk.GetPhysicalDeviceSurfaceFormats2KHR(physicalDevice, &surfaceInfo2, &formatCount, surfaceFormatsPtr);
					return surfaceFormats;
				}
			}

			[MustUseReturnValue]
			static VkPresentModeKHR[] GetPhysicalDeviceSurfacePresentModes(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface) {
				uint presentModeCount;
				Vk.GetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, null);
				if (presentModeCount == 0) { return Array.Empty<VkPresentModeKHR>(); }

				VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
				fixed (VkPresentModeKHR* presentModesPtr = presentModes) {
					Vk.GetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, &presentModeCount, presentModesPtr);
					return presentModes;
				}
			}
		}

		public delegate bool IsPhysicalDeviceSuitable(VkPhysicalDeviceProperties2 physicalDeviceProperties2, VkPhysicalDeviceFeatures2 physicalDeviceFeatures2);
		public delegate int RateGpuSuitability(PhysicalGpu physicalGpu);
	}
}