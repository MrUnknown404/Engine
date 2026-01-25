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
		private readonly DescriptorSetLayoutInfo[]? descriptorSetLayoutInfos;

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;
		private readonly uint maxFramesInFlight;

		private GraphicsPipeline(string debugName, VkDevice logicalDevice, VkPipeline pipeline, VkPipelineLayout layout, VkDescriptorPool? descriptorPool, VkDescriptorSetLayout? descriptorSetLayout,
			VkDescriptorSet[]? descriptorSets, DescriptorSetLayoutInfo[]? descriptorSetLayoutInfos, uint maxFramesInFlight) {
			DebugName = debugName;
			this.logicalDevice = logicalDevice;
			Pipeline = pipeline;
			Layout = layout;
			this.descriptorPool = descriptorPool;
			this.descriptorSetLayout = descriptorSetLayout;
			this.descriptorSets = descriptorSets;
			this.descriptorSetLayoutInfos = descriptorSetLayoutInfos;
			this.maxFramesInFlight = maxFramesInFlight;
		}

		[SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
		public void UpdateDescriptorSets() {
			if (descriptorSets == null || descriptorSetLayoutInfos == null) {
				Logger.Warn("Attempted to update descriptor sets when this graphics pipeline does not have any descriptor sets setup");
				return;
			}

			foreach (DescriptorSetLayoutInfo descriptorSetLayoutInfo in descriptorSetLayoutInfos) {
				VkWriteDescriptorSet[] writeDescriptorSets = descriptorSetLayoutInfo.WriteDescriptorSets;

				fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) {
					fixed (VkDescriptorBufferInfo* bufferInfoPtr = descriptorSetLayoutInfo.DescriptorBufferInfo) {
						fixed (VkDescriptorImageInfo* imageInfoPtr = descriptorSetLayoutInfo.DescriptorImageInfo) {
							for (int i = 0; i < maxFramesInFlight; i++) {
								writeDescriptorSetsPtr[i].dstSet = descriptorSets[i];

								switch (writeDescriptorSetsPtr[i].descriptorType) {
									case VkDescriptorType.DescriptorTypeUniformBuffer: writeDescriptorSetsPtr[i].pBufferInfo = &bufferInfoPtr[i]; break;
									case VkDescriptorType.DescriptorTypeCombinedImageSampler: writeDescriptorSetsPtr[i].pImageInfo = &imageInfoPtr[i]; break;
									default: throw new NotImplementedException();
								}
							}

							Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null);
						}
					}
				}
			}
		}

		public VkDescriptorSet GetCurrentDescriptorSet(byte currentFrame) => descriptorSets?[currentFrame] ?? throw new Engine3VulkanException("Cannot get descriptor set because this pipeline has no descriptor sets");

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			if (this.descriptorPool is { } descriptorPool) { Vk.DestroyDescriptorPool(logicalDevice, descriptorPool, null); }
			if (this.descriptorSetLayout is { } descriptorSetLayout) { Vk.DestroyDescriptorSetLayout(logicalDevice, descriptorSetLayout, null); }

			Vk.DestroyPipelineLayout(logicalDevice, Layout, null);
			Vk.DestroyPipeline(logicalDevice, Pipeline, null);

			WasDestroyed = true;
		}

		[PublicAPI]
		public class Builder {
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
			public List<VkDynamicState> DynamicStates { get; init; } = [ VkDynamicState.DynamicStateViewport, VkDynamicState.DynamicStateScissor, ];
			public uint MaxFramesInFlight { get; init; }

			private readonly string debugName;
			private readonly VkDevice logicalDevice;
			private readonly SwapChain swapChain;
			private readonly VkShaderObject[] shaders;
			private readonly VkVertexInputAttributeDescription[] vertexAttributeDescriptions;
			private readonly VkVertexInputBindingDescription[] vertexBindingDescriptions;

			public Builder(string debugName, VkDevice logicalDevice, SwapChain swapChain, VkShaderObject[] shaders, VkVertexInputAttributeDescription[] vertexAttributeDescriptions,
				VkVertexInputBindingDescription[] vertexBindingDescriptions) {
				this.debugName = debugName;
				this.logicalDevice = logicalDevice;
				this.swapChain = swapChain;
				this.shaders = shaders;
				this.vertexAttributeDescriptions = vertexAttributeDescriptions;
				this.vertexBindingDescriptions = vertexBindingDescriptions;
			}

			private readonly List<DescriptorSetLayoutInfo> descriptorSetLayoutInfos = new();

			public void AddDescriptorSet<T>(VkShaderStageFlagBits stageFlags, uint bindingLocation, T[] uniformBuffers, uint uniformBufferSize) where T : IVkBufferObject {
				if (MaxFramesInFlight == 0) { throw new Engine3VulkanException($"{nameof(MaxFramesInFlight)} cannot be zero when using descriptor sets"); }

				VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[MaxFramesInFlight];
				VkDescriptorBufferInfo[] descriptorBufferInfos = new VkDescriptorBufferInfo[MaxFramesInFlight];

				fixed (VkDescriptorBufferInfo* descriptorBufferInfosPtr = descriptorBufferInfos) {
					for (int i = 0; i < MaxFramesInFlight; i++) {
						descriptorBufferInfosPtr[i] = new() { buffer = uniformBuffers[i].Buffer, offset = 0, range = uniformBufferSize, };
						writeDescriptorSets[i] = new() { dstBinding = bindingLocation, dstArrayElement = 0, descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, };
					}
				}

				descriptorSetLayoutInfos.Add(new(VkDescriptorType.DescriptorTypeUniformBuffer, stageFlags, bindingLocation, writeDescriptorSets) { DescriptorBufferInfo = descriptorBufferInfos, });
			}

			public void AddDescriptorSet(VkShaderStageFlagBits stageFlags, uint bindingLocation, VkImageView imageView, VkSampler textureSampler) {
				if (MaxFramesInFlight == 0) { throw new Engine3VulkanException($"{nameof(MaxFramesInFlight)} cannot be zero when using descriptor sets"); }

				VkWriteDescriptorSet[] writeDescriptorSets = new VkWriteDescriptorSet[MaxFramesInFlight];
				VkDescriptorImageInfo[] descriptorImageInfos = new VkDescriptorImageInfo[MaxFramesInFlight];

				fixed (VkDescriptorImageInfo* descriptorImageInfosPtr = descriptorImageInfos) {
					for (int i = 0; i < MaxFramesInFlight; i++) {
						descriptorImageInfosPtr[i] = new() { imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal, imageView = imageView, sampler = textureSampler, };
						writeDescriptorSets[i] = new() { dstBinding = bindingLocation, dstArrayElement = 0, descriptorType = VkDescriptorType.DescriptorTypeCombinedImageSampler, descriptorCount = 1, };
					}
				}

				descriptorSetLayoutInfos.Add(new(VkDescriptorType.DescriptorTypeCombinedImageSampler, stageFlags, bindingLocation, writeDescriptorSets) { DescriptorImageInfo = descriptorImageInfos, });
			}

			public GraphicsPipeline MakePipeline() {
				byte* entryPointName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("main"u8));
				VkPipelineShaderStageCreateInfo[] shaderStageCreateInfos = new VkPipelineShaderStageCreateInfo[shaders.Length];
				for (int i = 0; i < shaders.Length; i++) {
					VkShaderObject shader = shaders[i];
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

				VkFormat swapChainImageFormat = swapChain.ImageFormat;
				VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new() { topology = Topology, };
				VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new() { viewportCount = 1, scissorCount = 1, };
				VkPipelineRenderingCreateInfo renderingCreateInfo = new() { colorAttachmentCount = 1, pColorAttachmentFormats = &swapChainImageFormat, };

				VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new() {
						depthClampEnable = (int)Vk.False,
						rasterizerDiscardEnable = (int)Vk.False,
						polygonMode = PolygonMode,
						lineWidth = 1,
						cullMode = CullMode,
						frontFace = FrontFace,
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
						srcColorBlendFactor = SrcColorBlendFactor,
						dstColorBlendFactor = DstColorBlendFactor,
						colorBlendOp = ColorBlendOp,
						srcAlphaBlendFactor = SrcAlphaBlendFactor,
						dstAlphaBlendFactor = DstAlphaBlendFactor,
						alphaBlendOp = AlphaBlendOp,
				};

				VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new() { logicOpEnable = (int)Vk.False, logicOp = VkLogicOp.LogicOpCopy, attachmentCount = 1, pAttachments = &colorBlendAttachmentState, };
				// colorBlendStateCreateInfo.blendConstants[0] = 0; // is there a better way of initializing this?
				// colorBlendStateCreateInfo.blendConstants[1] = 0;
				// colorBlendStateCreateInfo.blendConstants[2] = 0;
				// colorBlendStateCreateInfo.blendConstants[3] = 0;

				VkDescriptorPool? descriptorPool = null;
				VkDescriptorSetLayout? descriptorSetLayout = null;
				VkDescriptorSet[]? descriptorSets = null;

				VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new() { pushConstantRangeCount = 0, pPushConstantRanges = null, };
				if (descriptorSetLayoutInfos.Count != 0) {
					descriptorPool = CreateDescriptorPool();
					descriptorSetLayout = CreateDescriptorSetLayout();
					descriptorSets = AllocateDescriptorSets(descriptorPool.Value, descriptorSetLayout.Value);
					// UpdateDescriptorSets(descriptorSets);

					if (descriptorSetLayout is { } layout) {
						pipelineLayoutCreateInfo.setLayoutCount = 1;
						pipelineLayoutCreateInfo.pSetLayouts = &layout;
					}
				}

				VkPipelineLayout pipelineLayout;
				VkH.CheckIfSuccess(Vk.CreatePipelineLayout(logicalDevice, &pipelineLayoutCreateInfo, null, &pipelineLayout), VulkanException.Reason.CreatePipelineLayout);

				fixed (VkPipelineShaderStageCreateInfo* shaderStageCreateInfosPtr = shaderStageCreateInfos) {
					fixed (VkDynamicState* dynamicStatesPtr = DynamicStates.ToArray()) {
						fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = vertexAttributeDescriptions) {
							fixed (VkVertexInputBindingDescription* vertexBindingDescriptionPtr = vertexBindingDescriptions) {
								VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new() { dynamicStateCount = (uint)DynamicStates.Count, pDynamicStates = dynamicStatesPtr, };

								VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new() { // TODO can we replace this with shader buffers? like OpenGL vertex pulling
										vertexBindingDescriptionCount = (uint)vertexBindingDescriptions.Length,
										pVertexBindingDescriptions = vertexBindingDescriptionPtr,
										vertexAttributeDescriptionCount = (uint)vertexAttributeDescriptions.Length,
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

								return new(debugName, logicalDevice, graphicsPipeline, pipelineLayout, descriptorPool, descriptorSetLayout, descriptorSets, descriptorSetLayoutInfos.ToArray(), MaxFramesInFlight);
							}
						}
					}
				}

				[MustUseReturnValue]
				VkDescriptorPool CreateDescriptorPool() {
					VkDescriptorPoolSize[] poolSizes = new VkDescriptorPoolSize[descriptorSetLayoutInfos.Count];
					for (int i = 0; i < poolSizes.Length; i++) { poolSizes[i] = new() { type = descriptorSetLayoutInfos[i].DescriptorType, descriptorCount = MaxFramesInFlight, }; }

					fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes) {
						VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)poolSizes.Length, pPoolSizes = poolSizesPtr, maxSets = MaxFramesInFlight, };
						VkDescriptorPool descriptorPool;
						VkH.CheckIfSuccess(Vk.CreateDescriptorPool(logicalDevice, &poolCreateInfo, null, &descriptorPool), VulkanException.Reason.CreateDescriptorPool);
						return descriptorPool;
					}
				}

				[MustUseReturnValue]
				VkDescriptorSetLayout CreateDescriptorSetLayout() {
					VkDescriptorSetLayoutBinding[] bindings = new VkDescriptorSetLayoutBinding[descriptorSetLayoutInfos.Count];
					for (int i = 0; i < bindings.Length; i++) {
						DescriptorSetLayoutInfo binding = descriptorSetLayoutInfos[i];
						bindings[i] = new() { binding = binding.BindingLocation, descriptorType = binding.DescriptorType, stageFlags = binding.StageFlags, descriptorCount = 1, };
					}

					fixed (VkDescriptorSetLayoutBinding* bindingsPtr = bindings) {
						VkDescriptorSetLayoutCreateInfo layoutCreateInfo = new() { bindingCount = (uint)bindings.Length, pBindings = bindingsPtr, };
						VkDescriptorSetLayout descriptorSetLayout;
						VkH.CheckIfSuccess(Vk.CreateDescriptorSetLayout(logicalDevice, &layoutCreateInfo, null, &descriptorSetLayout), VulkanException.Reason.CreateDescriptorSetLayout);
						return descriptorSetLayout;
					}
				}

				[MustUseReturnValue]
				VkDescriptorSet[] AllocateDescriptorSets(VkDescriptorPool descriptorPool, VkDescriptorSetLayout descriptorSetLayout) {
					VkDescriptorSetLayout[] layouts = new VkDescriptorSetLayout[MaxFramesInFlight];
					for (int i = 0; i < layouts.Length; i++) { layouts[i] = descriptorSetLayout; }

					VkDescriptorSet[] descriptorSets = new VkDescriptorSet[MaxFramesInFlight];
					fixed (VkDescriptorSetLayout* layoutsPtr = layouts) {
						fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
							VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = descriptorPool, descriptorSetCount = (uint)descriptorSets.Length, pSetLayouts = layoutsPtr, };
							VkH.CheckIfSuccess(Vk.AllocateDescriptorSets(logicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr), VulkanException.Reason.AllocateDescriptorSets);
						}
					}

					return descriptorSets;
				}
			}
		}

		private readonly record struct DescriptorSetLayoutInfo {
			public required VkDescriptorType DescriptorType { get; init; }
			public required VkShaderStageFlagBits StageFlags { get; init; }
			public required uint BindingLocation { get; init; }
			public required VkWriteDescriptorSet[] WriteDescriptorSets { get; init; }

			public VkDescriptorBufferInfo[] DescriptorBufferInfo { get; init; } = Array.Empty<VkDescriptorBufferInfo>();
			public VkDescriptorImageInfo[] DescriptorImageInfo { get; init; } = Array.Empty<VkDescriptorImageInfo>();

			[SetsRequiredMembers]
			public DescriptorSetLayoutInfo(VkDescriptorType descriptorType, VkShaderStageFlagBits stageFlags, uint bindingLocation, VkWriteDescriptorSet[] writeDescriptorSets) {
				DescriptorType = descriptorType;
				StageFlags = stageFlags;
				BindingLocation = bindingLocation;
				WriteDescriptorSets = writeDescriptorSets;
			}
		}
	}
}