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
using Silk.NET.Shaderc;
using ZLinq;
using Compiler = Silk.NET.Shaderc.Compiler;
using ShaderKind = Silk.NET.Shaderc.ShaderKind;
using SourceLanguage = Silk.NET.Shaderc.SourceLanguage;

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
			VkResult result = Vk.CreateInstance(&instanceCreateInfo, null, &vkInstance);

			MarshalTk.FreeStringArrayCoTaskMem(requiredExtensionsPtr, requiredInstanceExtensions.Length);
#if DEBUG
			MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

			return result != VkResult.Success ? throw new VulkanException($"Failed to create Vulkan instance. {result}") : vkInstance;
		}

		[MustUseReturnValue]
		public static VkSurfaceKHR CreateSurface(VkInstance vkInstance, WindowHandle windowHandle) {
			VkResult result = Toolkit.Vulkan.CreateWindowSurface(vkInstance, windowHandle, null, out VkSurfaceKHR surface);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create surface. {result}") : surface;
		}

		[MustUseReturnValue]
		public static VkDevice CreateLogicalDevice(VkPhysicalDevice physicalDevice, QueueFamilyIndices queueFamilyIndices, string[] requiredDeviceExtensions, string[] requiredValidationLayers) {
			List<VkDeviceQueueCreateInfo> queueCreateInfos = new();
			float queuePriority = 1f;

			// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator // CS0212
			foreach (uint queueFamily in new HashSet<uint> { queueFamilyIndices.GraphicsFamily, queueFamilyIndices.PresentFamily, queueFamilyIndices.TransferFamily, }) {
				queueCreateInfos.Add(new() { queueFamilyIndex = queueFamily, queueCount = 1, pQueuePriorities = &queuePriority, });
			}

			VkPhysicalDeviceVulkan11Features physicalDeviceVulkan11Features = new(); // atm i'm not using most of these. but i may?
			VkPhysicalDeviceVulkan12Features physicalDeviceVulkan12Features = new() { pNext = &physicalDeviceVulkan11Features, };
			VkPhysicalDeviceVulkan13Features physicalDeviceVulkan13Features = new() { pNext = &physicalDeviceVulkan12Features, synchronization2 = (int)Vk.True, dynamicRendering = (int)Vk.True, };
			VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = new() { pNext = &physicalDeviceVulkan13Features, };
			VkPhysicalDeviceFeatures deviceFeatures = new(); // ???

			IntPtr requiredDeviceExtensionsPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredDeviceExtensions);
#if DEBUG
			IntPtr requiredValidationLayersPtr = MarshalTk.StringArrayToCoTaskMemAnsi(requiredValidationLayers);
#endif

			fixed (VkDeviceQueueCreateInfo* queueCreateInfosPtr = CollectionsMarshal.AsSpan(queueCreateInfos)) {
				VkDeviceCreateInfo deviceCreateInfo = new() {
						pNext = &physicalDeviceVulkan14Features,
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

				VkDevice logicalDevice;
				VkResult result = Vk.CreateDevice(physicalDevice, &deviceCreateInfo, null, &logicalDevice);

				MarshalTk.FreeStringArrayCoTaskMem(requiredDeviceExtensionsPtr, requiredDeviceExtensions.Length);
#if DEBUG
				MarshalTk.FreeStringArrayCoTaskMem(requiredValidationLayersPtr, requiredValidationLayers.Length);
#endif

				return result != VkResult.Success ? throw new VulkanException($"Failed to create logical device. {result}") : logicalDevice;
			}
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
				if (physicalDeviceExtensionProperties.Length == 0) { continue; }
				if (!CheckDeviceExtensionSupport(physicalDeviceExtensionProperties, requiredDeviceExtensions)) { continue; }
				if (!FindQueueFamilies(physicalDevice, surface, out uint? graphicsFamily, out uint? presentFamily, out uint? transferFamily)) { continue; }
				if (!QuerySwapChainSupport(physicalDevice, surface, out _, out _, out _)) { continue; }

				gpus.Add(new(physicalDevice, physicalDeviceProperties2, physicalDeviceFeatures2, physicalDeviceExtensionProperties, new(graphicsFamily.Value, presentFamily.Value, transferFamily.Value)));
			}

			return gpus.ToArray();

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
		public static PhysicalGpu PickBestGpu(PhysicalGpu[] physicalGpus, RateGpuSuitability rateGpuSuitability) {
			PhysicalGpu? bestDevice = null;
			int bestDeviceScore = int.MinValue;

			foreach (PhysicalGpu device in physicalGpus) {
				int score = rateGpuSuitability(device);
				if (score > bestDeviceScore) {
					bestDevice = device;
					bestDeviceScore = score;
				}
			}

			return bestDevice ?? throw new VulkanException("Failed to find any gpus");
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

		public static void CreateSwapChain(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkSurfaceKHR surface, QueueFamilyIndices queueFamilyIndices, Vector2i framebufferSize, out VkSwapchainKHR swapChain,
			out VkExtent2D swapChainExtent, out VkFormat swapChainImageFormat, VkPresentModeKHR presentMode = VkPresentModeKHR.PresentModeMailboxKhr, VkSurfaceTransformFlagBitsKHR? surfaceTransform = null,
			VkSwapchainKHR? oldSwapChain = null) {
			if (!QuerySwapChainSupport(physicalDevice, surface, out VkSurfaceCapabilities2KHR surfaceCapabilities2, out VkSurfaceFormat2KHR[]? surfaceFormats2, out VkPresentModeKHR[]? supportedPresentModes)) {
				throw new VulkanException("Failed to query swap chain support");
			}

			if (!supportedPresentModes.Contains(presentMode)) { throw new VulkanException("Surface does not support requested present mode"); }
			if (ChooseSwapSurfaceFormat(surfaceFormats2) is not { } surfaceFormat2) { throw new VulkanException("Could not find any valid surface formats"); }

			VkSurfaceCapabilitiesKHR surfaceCapabilities = surfaceCapabilities2.surfaceCapabilities;
			swapChainImageFormat = surfaceFormat2.surfaceFormat.format;
			swapChainExtent = ChooseSwapExtent(framebufferSize, surfaceCapabilities);

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

			fixed (uint* pQueueFamilyIndicesPtr = queueFamilyIndicesArray) {
				VkSwapchainCreateInfoKHR createInfo = new() {
						surface = surface,
						imageFormat = swapChainImageFormat,
						imageColorSpace = surfaceFormat2.surfaceFormat.colorSpace,
						imageExtent = swapChainExtent,
						minImageCount = imageCount,
						imageArrayLayers = 1,
						imageUsage = VkImageUsageFlagBits.ImageUsageColorAttachmentBit,
						imageSharingMode = imageSharingMode,
						queueFamilyIndexCount = queueFamilyIndexCount,
						pQueueFamilyIndices = queueFamilyIndicesArray.Length == 0 ? null : pQueueFamilyIndicesPtr,
						preTransform = surfaceTransform ?? surfaceCapabilities.currentTransform,
						compositeAlpha = VkCompositeAlphaFlagBitsKHR.CompositeAlphaOpaqueBitKhr,
						presentMode = presentMode,
						clipped = (int)Vk.True,
						oldSwapchain = oldSwapChain ?? VkSwapchainKHR.Zero,
				};

				VkSwapchainKHR tempSwapChain;
				VkResult result = Vk.CreateSwapchainKHR(logicalDevice, &createInfo, null, &tempSwapChain);
				if (result != VkResult.Success) { throw new VulkanException($"Failed to create swap chain. {result}"); }

				swapChain = tempSwapChain;
			}

			return;

			[MustUseReturnValue]
			static VkSurfaceFormat2KHR? ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormat2KHR> availableFormats) =>
					availableFormats.Where(static format => format.surfaceFormat is { format: VkFormat.FormatB8g8r8a8Srgb, colorSpace: VkColorSpaceKHR.ColorSpaceSrgbNonlinearKhr, }).Cast<VkSurfaceFormat2KHR?>().FirstOrDefault();

			[MustUseReturnValue]
			static VkExtent2D ChooseSwapExtent(Vector2i framebufferSize, VkSurfaceCapabilitiesKHR surfaceCapabilities) =>
					surfaceCapabilities.currentExtent.width != uint.MaxValue ?
							surfaceCapabilities.currentExtent :
							new() {
									width = Math.Clamp((uint)framebufferSize.X, surfaceCapabilities.minImageExtent.width, surfaceCapabilities.maxImageExtent.width),
									height = Math.Clamp((uint)framebufferSize.Y, surfaceCapabilities.minImageExtent.height, surfaceCapabilities.maxImageExtent.height),
							};
		}

		[MustUseReturnValue]
		public static VkImage[] GetSwapChainImages(VkDevice logicalDevice, VkSwapchainKHR swapChain) {
			uint swapChainImageCount;
			Vk.GetSwapchainImagesKHR(logicalDevice, swapChain, &swapChainImageCount, null);

			VkImage[] swapChainImages = new VkImage[swapChainImageCount];
			fixed (VkImage* swapChainImagesPtr = swapChainImages) {
				VkResult result = Vk.GetSwapchainImagesKHR(logicalDevice, swapChain, &swapChainImageCount, swapChainImagesPtr);
				return result != VkResult.Success ? throw new VulkanException($"Failed to get swap chain images. {result}") : swapChainImages;
			}
		}

		[MustUseReturnValue]
		public static VkImageView[] CreateImageViews(VkDevice logicalDevice, VkImage[] swapChainImages, VkFormat swapChainFormat) {
			VkImageView[] imageViews = new VkImageView[swapChainImages.Length];

			fixed (VkImageView* imageViewsPtr = imageViews) {
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

					VkResult result = Vk.CreateImageView(logicalDevice, &createInfo, null, &imageViewsPtr[i]);
					if (result != VkResult.Success) { throw new VulkanException($"Failed to create swap chain image view {i}. {result}"); }
				}
			}

			return imageViews;
		}

		[MustUseReturnValue]
		public static bool CheckSupportForRequiredInstanceExtensions(VkExtensionProperties[] instanceExtensionProperties, string[] requiredInstanceExtensions) =>
				requiredInstanceExtensions.All(wantedExtension => instanceExtensionProperties.Any(extensionProperties => {
					ReadOnlySpan<byte> extensionName = extensionProperties.extensionName;
					return Encoding.UTF8.GetString(extensionName[..extensionName.IndexOf((byte)0)]) == wantedExtension;
				}));

		[MustUseReturnValue]
		public static VkQueue GetDeviceQueue(VkDevice logicalDevice, uint queueFamilyIndex) {
			VkDeviceQueueInfo2 deviceQueueInfo2 = new() { queueFamilyIndex = queueFamilyIndex, };
			VkQueue queue;
			Vk.GetDeviceQueue2(logicalDevice, &deviceQueueInfo2, &queue);
			return queue;
		}

		[MustUseReturnValue]
		public static VkShaderModule CreateShaderModule(VkDevice logicalDevice, string fileLocation, ShaderLanguage shaderLang, ShaderType shaderType, Assembly assembly) {
			string fullFileName = $"{fileLocation}.{shaderType.FileExtension}.{shaderLang.FileExtension}";

			using Stream? shaderStream = AssetH.GetAssetStream($"Shaders.{fullFileName}", assembly);
			if (shaderStream == null) { throw new Engine3Exception("Failed to create asset stream"); }

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

					CompilationResult* compilationResult = shaderc.CompileIntoSpv(compiler, sourcePtr, (nuint)source.Length, shaderKind, shaderNamePtr, "main", options);
					shaderc.CompileOptionsRelease(options);

					CompilationStatus status = shaderc.ResultGetCompilationStatus(compilationResult);
					shaderc.CompilerRelease(compiler);

					if (status != CompilationStatus.Success) {
						shaderc.ResultRelease(compilationResult);
						throw new Engine3Exception($"Failed to compile {shaderType} shader: {fileLocation}. {shaderc.ResultGetErrorMessageS(compilationResult)}");
					}

					VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = shaderc.ResultGetLength(compilationResult), pCode = (uint*)shaderc.ResultGetBytes(compilationResult), };
					VkShaderModule shaderModule;
					VkResult result = Vk.CreateShaderModule(logicalDevice, &shaderModuleCreateInfo, null, &shaderModule);

					shaderc.ResultRelease(compilationResult);

					return result != VkResult.Success ? throw new VulkanException($"Failed to create shader module. {result}") : shaderModule;
				}
				case ShaderLanguage.SpirV: {
					using BinaryReader reader = new(shaderStream);
					byte[] data = reader.ReadBytes((int)shaderStream.Length);

					fixed (byte* shaderCodePtr = data) {
						VkShaderModuleCreateInfo shaderModuleCreateInfo = new() { codeSize = (UIntPtr)data.Length, pCode = (uint*)shaderCodePtr, };
						VkShaderModule shaderModule;
						VkResult result = Vk.CreateShaderModule(logicalDevice, &shaderModuleCreateInfo, null, &shaderModule);
						return result != VkResult.Success ? throw new VulkanException($"Failed to create shader module. {result}") : shaderModule;
					}
				}
				default: throw new ArgumentOutOfRangeException(nameof(shaderLang), shaderLang, null);
			}
		}

		[MustUseReturnValue]
		public static VkCommandPool CreateCommandPool(VkDevice logicalDevice, VkCommandPoolCreateFlagBits commandPoolCreateFlags, uint queueFamilyIndex) {
			VkCommandPoolCreateInfo commandPoolCreateInfo = new() { flags = commandPoolCreateFlags, queueFamilyIndex = queueFamilyIndex, };
			VkCommandPool commandPool;
			VkResult result = Vk.CreateCommandPool(logicalDevice, &commandPoolCreateInfo, null, &commandPool);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create command pool. {result}") : commandPool;
		}

		[MustUseReturnValue]
		public static VkCommandBuffer[] CreateCommandBuffers(VkDevice logicalDevice, VkCommandPool commandPool, uint count, VkCommandBufferLevel level = VkCommandBufferLevel.CommandBufferLevelPrimary) {
			VkCommandBufferAllocateInfo commandBufferAllocateInfo = new() { commandPool = commandPool, level = level, commandBufferCount = count, };
			VkCommandBuffer[] commandBuffers = new VkCommandBuffer[count];
			fixed (VkCommandBuffer* commandBuffersPtr = commandBuffers) {
				VkResult result = Vk.AllocateCommandBuffers(logicalDevice, &commandBufferAllocateInfo, commandBuffersPtr);
				return result != VkResult.Success ? throw new VulkanException($"Failed to create command buffers. {result}") : commandBuffers;
			}
		}

		[MustUseReturnValue]
		public static VkSemaphore[] CreateSemaphores(VkDevice logicalDevice, uint count) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore[] semaphores = new VkSemaphore[count];

			fixed (VkSemaphore* semaphoresPtr = semaphores) {
				for (int i = 0; i < count; i++) {
					VkResult result = Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphoresPtr[i]);
					if (result != VkResult.Success) { throw new VulkanException($"Failed to create semaphore. {result}"); }
				}
			}

			return semaphores;
		}

		[MustUseReturnValue]
		public static VkSemaphore CreateSemaphore(VkDevice logicalDevice) {
			VkSemaphoreCreateInfo semaphoreCreateInfo = new();
			VkSemaphore semaphore;
			VkResult result = Vk.CreateSemaphore(logicalDevice, &semaphoreCreateInfo, null, &semaphore);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create semaphore. {result}") : semaphore;
		}

		[MustUseReturnValue]
		public static VkFence[] CreateFences(VkDevice logicalDevice, uint count) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence[] fences = new VkFence[count];

			fixed (VkFence* fencesPtr = fences) {
				for (int i = 0; i < count; i++) {
					VkResult result = Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fencesPtr[i]);
					if (result != VkResult.Success) { throw new VulkanException($"Failed to create fence. {result}"); }
				}
			}

			return fences;
		}

		[MustUseReturnValue]
		public static VkFence CreateFence(VkDevice logicalDevice) {
			VkFenceCreateInfo fenceCreateInfo = new() { flags = VkFenceCreateFlagBits.FenceCreateSignaledBit, };
			VkFence fence;
			VkResult result = Vk.CreateFence(logicalDevice, &fenceCreateInfo, null, &fence);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create fence. {result}") : fence;
		}

		public static void SubmitQueues(VkQueue queue, VkSubmitInfo[] submitInfos, VkFence? fence) {
			fixed (VkSubmitInfo* submitInfosPtr = submitInfos) {
				VkResult result = Vk.QueueSubmit(queue, (uint)submitInfos.Length, submitInfosPtr, fence ?? VkFence.Zero);
				if (result != VkResult.Success) { throw new VulkanException($"Failed to submit queue. {result}"); }
			}
		}

		public static void SubmitQueue(VkQueue queue, VkSubmitInfo submitInfo, VkFence? fence) {
			VkResult result = Vk.QueueSubmit(queue, 1, &submitInfo, fence ?? VkFence.Zero);
			if (result != VkResult.Success) { throw new VulkanException($"Failed to submit queue. {result}"); } // TODO device lost?
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
		public static VkBuffer CreateBuffer(VkDevice logicalDevice, VkBufferCreateInfo bufferCreateInfo) {
			VkBuffer buffer;
			VkResult result = Vk.CreateBuffer(logicalDevice, &bufferCreateInfo, null, &buffer);
			return result != VkResult.Success ? throw new VulkanException($"Failed to create buffer. {result}") : buffer;
		}

		[MustUseReturnValue]
		public static VkDeviceMemory CreateDeviceMemory(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkBuffer buffer, VkMemoryPropertyFlagBits memoryPropertyFlags) {
			VkBufferMemoryRequirementsInfo2 bufferMemoryRequirementsInfo2 = new() { buffer = buffer, };
			VkMemoryRequirements2 memoryRequirements2 = new();
			Vk.GetBufferMemoryRequirements2(logicalDevice, &bufferMemoryRequirementsInfo2, &memoryRequirements2);
			VkMemoryRequirements memoryRequirements = memoryRequirements2.memoryRequirements;

			VkMemoryAllocateInfo memoryAllocateInfo = new() { allocationSize = memoryRequirements.size, memoryTypeIndex = FindMemoryType(physicalDevice, memoryRequirements.memoryTypeBits, memoryPropertyFlags), };
			VkDeviceMemory deviceMemory;

			// TODO "It should be noted that in a real world application, you're not supposed to actually call vkAllocateMemory for every individual buffer."
			// "The right way to allocate memory for a large number of objects at the same time is to create a custom allocator that splits up a single allocation among many different objects by using the offset parameters that we've seen in many functions."
			VkResult result = Vk.AllocateMemory(logicalDevice, &memoryAllocateInfo, null, &deviceMemory);
			return result != VkResult.Success ? throw new VulkanException($"Failed to allocate memory. {result}") : deviceMemory;
		}

		public static VkResult BeginCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferUsageFlagBits commandBufferUsageFlags = 0) {
			VkCommandBufferBeginInfo commandBufferBeginInfo = new() { flags = commandBufferUsageFlags, };
			return Vk.BeginCommandBuffer(commandBuffer, &commandBufferBeginInfo);
		}

		[MustUseReturnValue]
		private static uint FindMemoryType(VkPhysicalDevice physicalDevice, uint typeFilter, VkMemoryPropertyFlagBits memoryPropertyFlag) {
			VkPhysicalDeviceMemoryProperties2 memoryProperties2 = new();
			Vk.GetPhysicalDeviceMemoryProperties2(physicalDevice, &memoryProperties2);
			VkPhysicalDeviceMemoryProperties memoryProperties = memoryProperties2.memoryProperties;

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