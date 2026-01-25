using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class GraphicsPipeline : IGraphicsResource {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public VkPipeline Pipeline { get; }
		public VkPipelineLayout Layout { get; }

		private readonly VkDescriptorPool? descriptorPool;
		private readonly VkDescriptorSetLayout? descriptorSetLayout;
		private readonly VkDescriptorSet[]? descriptorSets;

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;
		private readonly byte maxFramesInFlight;

		public GraphicsPipeline(VkDevice logicalDevice, Settings settings) {
			DebugName = settings.DebugName;
			Pipeline = CreateGraphicsPipeline(logicalDevice, settings, out VkPipelineLayout layout, out VkDescriptorPool? descriptorPool, out VkDescriptorSetLayout? descriptorSetLayout);
			Layout = layout;
			this.logicalDevice = logicalDevice;
			maxFramesInFlight = settings.MaxFramesInFlight;

			if (descriptorPool != null && descriptorSetLayout != null) {
				this.descriptorPool = descriptorPool;
				this.descriptorSetLayout = descriptorSetLayout;
				descriptorSets = AllocateDescriptorSets(logicalDevice, descriptorPool.Value, descriptorSetLayout.Value, maxFramesInFlight);
			}
		}

		public void UpdateDescriptorSet(uint binding, VkBuffer[] buffer, ulong range, ulong offset = 0) {
			if (descriptorSets == null) {
				Logger.Warn("Attempted to update descriptor sets when this graphics pipeline does not use any descriptor sets");
				return;
			}

			VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorBufferInfo[] bufferInfos = new VkDescriptorBufferInfo[maxFramesInFlight];

			fixed (VkDescriptorBufferInfo* bufferInfosPtr = bufferInfos) {
				for (int i = 0; i < maxFramesInFlight; i++) {
					bufferInfosPtr[i] = new() { buffer = buffer[i], offset = offset, range = range, };
					writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, pBufferInfo = &bufferInfosPtr[i], };
				}

				fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
			}
		}

		public void UpdateDescriptorSet<T>(uint binding, T[] buffer, ulong range, ulong offset = 0) where T : IVkBufferObject {
			if (descriptorSets == null) {
				Logger.Warn("Attempted to update descriptor sets when this graphics pipeline does use any descriptor sets ");
				return;
			}

			VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorBufferInfo[] bufferInfos = new VkDescriptorBufferInfo[maxFramesInFlight];

			fixed (VkDescriptorBufferInfo* bufferInfosPtr = bufferInfos) {
				for (int i = 0; i < maxFramesInFlight; i++) {
					bufferInfosPtr[i] = new() { buffer = buffer[i].Buffer, offset = offset, range = range, };
					writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, pBufferInfo = &bufferInfosPtr[i], };
				}

				fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
			}
		}

		public void UpdateDescriptorSet(uint binding, VkImageView imageView, VkSampler textureSampler) {
			if (descriptorSets == null) {
				Logger.Warn("Attempted to update descriptor sets when this graphics pipeline does not have any descriptor sets setup");
				return;
			}

			VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[maxFramesInFlight];
			VkDescriptorImageInfo imageInfo = new() { imageView = imageView, imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal, sampler = textureSampler, };

			for (int i = 0; i < maxFramesInFlight; i++) {
				writeDescriptorSets[i] = new() { dstBinding = binding, dstSet = descriptorSets[i], descriptorType = VkDescriptorType.DescriptorTypeCombinedImageSampler, descriptorCount = 1, pImageInfo = &imageInfo, };
			}

			fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
		}

		public VkDescriptorSet GetDescriptorSet(byte currentFrame) => descriptorSets?[currentFrame] ?? throw new Engine3VulkanException("Cannot get descriptor set because this pipeline has no descriptor sets");

		[MustUseReturnValue]
		private static VkDescriptorSet[] AllocateDescriptorSets(VkDevice logicalDevice, VkDescriptorPool descriptorPool, VkDescriptorSetLayout descriptorSetLayout, byte maxFramesInFlight) {
			VkDescriptorSetLayout[] layouts = new VkDescriptorSetLayout[maxFramesInFlight];
			for (int i = 0; i < layouts.Length; i++) { layouts[i] = descriptorSetLayout; }

			VkDescriptorSet[] descriptorSets = new VkDescriptorSet[maxFramesInFlight];
			fixed (VkDescriptorSetLayout* layoutsPtr = layouts) {
				fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
					VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = descriptorPool, descriptorSetCount = (uint)descriptorSets.Length, pSetLayouts = layoutsPtr, };
					VkH.CheckIfSuccess(Vk.AllocateDescriptorSets(logicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr), VulkanException.Reason.AllocateDescriptorSets);
				}
			}

			return descriptorSets;
		}

		[MustUseReturnValue]
		private static VkPipeline CreateGraphicsPipeline(VkDevice logicalDevice, Settings settings, out VkPipelineLayout pipelineLayout, out VkDescriptorPool? descriptorPool, out VkDescriptorSetLayout? descriptorSetLayout) {
			byte* entryPointName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("main"u8));
			VkPipelineShaderStageCreateInfo[] shaderStageCreateInfos = new VkPipelineShaderStageCreateInfo[settings.Shaders.Length];
			for (int i = 0; i < settings.Shaders.Length; i++) {
				VkShaderObject shader = settings.Shaders[i];
				shaderStageCreateInfos[i] = new() {
						module = shader.ShaderModule,
						stage = shader.ShaderType switch {
								ShaderType.Fragment => VkShaderStageFlagBits.ShaderStageFragmentBit,
								ShaderType.Vertex => VkShaderStageFlagBits.ShaderStageVertexBit,
								ShaderType.Geometry => VkShaderStageFlagBits.ShaderStageGeometryBit,
								ShaderType.TessEvaluation => VkShaderStageFlagBits.ShaderStageTessellationEvaluationBit,
								ShaderType.TessControl => VkShaderStageFlagBits.ShaderStageTessellationControlBit,
								ShaderType.Compute => VkShaderStageFlagBits.ShaderStageComputeBit,
								_ => throw new ArgumentOutOfRangeException(),
						},
						pName = entryPointName,
				};
			}

			VkFormat swapChainImageFormat = settings.SwapChainImageFormat;
			VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new() { topology = settings.Topology, };
			VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new() { viewportCount = 1, scissorCount = 1, };
			VkPipelineRenderingCreateInfo renderingCreateInfo = new() { colorAttachmentCount = 1, pColorAttachmentFormats = &swapChainImageFormat, };

			VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new() {
					depthClampEnable = (int)Vk.False,
					rasterizerDiscardEnable = (int)Vk.False,
					polygonMode = settings.PolygonMode,
					lineWidth = 1,
					cullMode = settings.CullMode,
					frontFace = settings.FrontFace,
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
					srcColorBlendFactor = settings.SrcColorBlendFactor,
					dstColorBlendFactor = settings.DstColorBlendFactor,
					colorBlendOp = settings.ColorBlendOp,
					srcAlphaBlendFactor = settings.SrcAlphaBlendFactor,
					dstAlphaBlendFactor = settings.DstAlphaBlendFactor,
					alphaBlendOp = settings.AlphaBlendOp,
			};

			VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new() { logicOpEnable = (int)Vk.False, logicOp = VkLogicOp.LogicOpCopy, attachmentCount = 1, pAttachments = &colorBlendAttachmentState, };
			// colorBlendStateCreateInfo.blendConstants[0] = 0; // is there a better way of initializing this?
			// colorBlendStateCreateInfo.blendConstants[1] = 0;
			// colorBlendStateCreateInfo.blendConstants[2] = 0;
			// colorBlendStateCreateInfo.blendConstants[3] = 0;

			VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new() { pushConstantRangeCount = 0, pPushConstantRanges = null, };
			if (settings.DescriptorSets != null) {
				descriptorPool = CreateDescriptorPool(logicalDevice, settings.DescriptorSets);
				descriptorSetLayout = CreateDescriptorSetLayout(logicalDevice, settings.DescriptorSets);
				VkDescriptorSetLayout tempDescriptorSetLayout = descriptorSetLayout.Value;

				pipelineLayoutCreateInfo.setLayoutCount = 1;
				pipelineLayoutCreateInfo.pSetLayouts = &tempDescriptorSetLayout; // shouldn't this pointer be invalid before vkCreatePipelineLayout is called? why isn't it?
			} else {
				descriptorPool = null;
				descriptorSetLayout = null;
			}

			VkPipelineLayout tempPipelineLayout;
			VkH.CheckIfSuccess(Vk.CreatePipelineLayout(logicalDevice, &pipelineLayoutCreateInfo, null, &tempPipelineLayout), VulkanException.Reason.CreatePipelineLayout);
			pipelineLayout = tempPipelineLayout;

			fixed (VkPipelineShaderStageCreateInfo* shaderStageCreateInfosPtr = shaderStageCreateInfos) {
				fixed (VkDynamicState* dynamicStatesPtr = settings.DynamicStates) {
					fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = settings.VertexAttributeDescriptions) {
						fixed (VkVertexInputBindingDescription* vertexBindingDescriptionPtr = settings.VertexBindingDescriptions) {
							VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new() { dynamicStateCount = (uint)settings.DynamicStates.Length, pDynamicStates = dynamicStatesPtr, };

							VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new() { // TODO can we replace this with shader buffers? like OpenGL vertex pulling
									vertexBindingDescriptionCount = (uint)settings.VertexBindingDescriptions.Length,
									pVertexBindingDescriptions = vertexBindingDescriptionPtr,
									vertexAttributeDescriptionCount = (uint)settings.VertexAttributeDescriptions.Length,
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

							VkPipeline graphicsPipeline;
							VkH.CheckIfSuccess(Vk.CreateGraphicsPipelines(logicalDevice, VkPipelineCache.Zero, 1, &pipelineCreateInfo, null, &graphicsPipeline), VulkanException.Reason.CreateGraphicsPipeline);
							return graphicsPipeline;
						}
					}
				}
			}

			[MustUseReturnValue]
			static VkDescriptorPool CreateDescriptorPool(VkDevice logicalDevice, DescriptorSet[] descriptorSets) {
				VkDescriptorPoolSize[] poolSizes = new VkDescriptorPoolSize[descriptorSets.Length];
				for (int i = 0; i < poolSizes.Length; i++) { poolSizes[i] = new() { type = descriptorSets[i].DescriptorType, descriptorCount = (uint)descriptorSets.Length, }; }

				fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes) {
					VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)poolSizes.Length, pPoolSizes = poolSizesPtr, maxSets = (uint)descriptorSets.Length, };
					VkDescriptorPool descriptorPool;
					VkH.CheckIfSuccess(Vk.CreateDescriptorPool(logicalDevice, &poolCreateInfo, null, &descriptorPool), VulkanException.Reason.CreateDescriptorPool);
					return descriptorPool;
				}
			}

			[MustUseReturnValue]
			static VkDescriptorSetLayout CreateDescriptorSetLayout(VkDevice logicalDevice, DescriptorSet[] descriptorSets) {
				VkDescriptorSetLayoutBinding[] bindings = new VkDescriptorSetLayoutBinding[descriptorSets.Length];
				for (int i = 0; i < bindings.Length; i++) {
					DescriptorSet binding = descriptorSets[i];
					bindings[i] = new() { binding = binding.BindingLocation, descriptorType = binding.DescriptorType, stageFlags = binding.StageFlags, descriptorCount = 1, };
				}

				fixed (VkDescriptorSetLayoutBinding* bindingsPtr = bindings) {
					VkDescriptorSetLayoutCreateInfo layoutCreateInfo = new() { bindingCount = (uint)bindings.Length, pBindings = bindingsPtr, };
					VkDescriptorSetLayout descriptorSetLayout;
					VkH.CheckIfSuccess(Vk.CreateDescriptorSetLayout(logicalDevice, &layoutCreateInfo, null, &descriptorSetLayout), VulkanException.Reason.CreateDescriptorSetLayout);
					return descriptorSetLayout;
				}
			}
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			if (this.descriptorPool is { } descriptorPool) { Vk.DestroyDescriptorPool(logicalDevice, descriptorPool, null); }
			if (this.descriptorSetLayout is { } descriptorSetLayout) { Vk.DestroyDescriptorSetLayout(logicalDevice, descriptorSetLayout, null); }

			Vk.DestroyPipelineLayout(logicalDevice, Layout, null);
			Vk.DestroyPipeline(logicalDevice, Pipeline, null);

			WasDestroyed = true;
		}

		public class Settings {
			public string DebugName { get; }
			public VkFormat SwapChainImageFormat { get; }
			public VkShaderObject[] Shaders { get; }
			public VkVertexInputAttributeDescription[] VertexAttributeDescriptions { get; }
			public VkVertexInputBindingDescription[] VertexBindingDescriptions { get; }

			public VkPrimitiveTopology Topology { get; init; } = VkPrimitiveTopology.PrimitiveTopologyTriangleList;
			public VkPolygonMode PolygonMode { get; init; } = VkPolygonMode.PolygonModeFill;
			public VkCullModeFlagBits CullMode { get; init; } = VkCullModeFlagBits.CullModeBackBit;
			public VkFrontFace FrontFace { get; init; } = VkFrontFace.FrontFaceClockwise;
			public VkBlendFactor SrcColorBlendFactor { get; init; } = VkBlendFactor.BlendFactorSrcAlpha;
			public VkBlendFactor DstColorBlendFactor { get; init; } = VkBlendFactor.BlendFactorOneMinusSrcAlpha;
			public VkBlendOp ColorBlendOp { get; init; } = VkBlendOp.BlendOpAdd;
			public VkBlendFactor SrcAlphaBlendFactor { get; init; } = VkBlendFactor.BlendFactorOne;
			public VkBlendFactor DstAlphaBlendFactor { get; init; } = VkBlendFactor.BlendFactorZero;
			public VkBlendOp AlphaBlendOp { get; init; } = VkBlendOp.BlendOpAdd;
			public VkDynamicState[] DynamicStates { get; init; } = [ VkDynamicState.DynamicStateViewport, VkDynamicState.DynamicStateScissor, ];

			public byte MaxFramesInFlight { get; private set; }
			public DescriptorSet[]? DescriptorSets { get; private set; }

			public Settings(string debugName, VkFormat swapChainImageFormat, VkShaderObject[] shaders, VkVertexInputAttributeDescription[] vertexAttributeDescriptions, VkVertexInputBindingDescription[] vertexBindingDescriptions) {
				DebugName = debugName;
				SwapChainImageFormat = swapChainImageFormat;
				Shaders = shaders;
				VertexAttributeDescriptions = vertexAttributeDescriptions;
				VertexBindingDescriptions = vertexBindingDescriptions;
			}

			public Settings SetDescriptorSets(DescriptorSet[] descriptorSets, byte maxFramesInFlight) {
				if (maxFramesInFlight == 0) { throw new Engine3VulkanException($"{nameof(MaxFramesInFlight)} cannot be zero when using descriptor sets"); }

				DescriptorSets = descriptorSets;
				MaxFramesInFlight = maxFramesInFlight;
				return this;
			}
		}

		public readonly record struct DescriptorSet {
			public required VkDescriptorType DescriptorType { get; init; }
			public required VkShaderStageFlagBits StageFlags { get; init; }
			public required uint BindingLocation { get; init; }

			[SetsRequiredMembers]
			public DescriptorSet(VkDescriptorType descriptorType, VkShaderStageFlagBits stageFlags, uint bindingLocation) {
				DescriptorType = descriptorType;
				StageFlags = stageFlags;
				BindingLocation = bindingLocation;
			}
		}
	}
}