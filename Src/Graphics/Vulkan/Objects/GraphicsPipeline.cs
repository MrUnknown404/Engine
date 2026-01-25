using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan.Objects {
	public unsafe class GraphicsPipeline : IGraphicsResource {
		public VkPipeline Pipeline { get; }
		public VkPipelineLayout Layout { get; }

		public VkDescriptorPool? DescriptorPool { get; }
		public VkDescriptorSetLayout? DescriptorSetLayout { get; }
		public VkDescriptorSet[]? DescriptorSets { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		private readonly VkDevice logicalDevice;

		private GraphicsPipeline(string debugName, VkDevice logicalDevice, VkPipeline pipeline, VkPipelineLayout layout, VkDescriptorPool? descriptorPool, VkDescriptorSetLayout? descriptorSetLayout,
			VkDescriptorSet[]? descriptorSets) {
			DebugName = debugName;
			this.logicalDevice = logicalDevice;
			Pipeline = pipeline;
			Layout = layout;
			DescriptorPool = descriptorPool;
			DescriptorSetLayout = descriptorSetLayout;
			DescriptorSets = descriptorSets;
		}

		public void Destroy() {
			if (IGraphicsResource.WarnIfDestroyed(this)) { return; }

			if (DescriptorPool is { } descriptorPool) { Vk.DestroyDescriptorPool(logicalDevice, descriptorPool, null); }
			if (DescriptorSetLayout is { } descriptorSetLayout) { Vk.DestroyDescriptorSetLayout(logicalDevice, descriptorSetLayout, null); }

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

			private VkDescriptorPool? descriptorPool;
			private VkDescriptorSetLayout? descriptorSetLayout;
			private VkDescriptorSet[]? descriptorSets;

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

			public void AddDescriptorSet<T>(DescriptorSetLayoutInfo descriptorSetLayoutInfo, uint maxFramesInFlight, T[] uniformBuffers, uint uniformBufferSize) where T : IVkBufferObject { }

			// TODO make it better way of adding desciptor sets
			[Obsolete("make better method")]
			public void AddDescriptorSets<T>(DescriptorSetLayoutInfo[] descriptorSetLayoutInfos, uint maxFramesInFlight, T[] uniformBuffers, uint uniformBufferSize, VkImageView imageView, VkSampler textureSampler)
					where T : IVkBufferObject {
				descriptorPool = CreateDescriptorPool(logicalDevice, descriptorSetLayoutInfos, maxFramesInFlight);
				descriptorSetLayout = CreateDescriptorSetLayout(logicalDevice, descriptorSetLayoutInfos);
				descriptorSets = CreateDescriptorSets(logicalDevice, descriptorPool.Value, descriptorSetLayout.Value, maxFramesInFlight, uniformBufferSize, uniformBuffers, imageView, textureSampler);

				return;

				[MustUseReturnValue]
				static VkDescriptorPool CreateDescriptorPool(VkDevice logicalDevice, DescriptorSetLayoutInfo[] descriptorSetLayoutInfos, uint maxFramesInFlight) {
					VkDescriptorPoolSize[] poolSizes = new VkDescriptorPoolSize[descriptorSetLayoutInfos.Length];
					for (int i = 0; i < poolSizes.Length; i++) { poolSizes[i] = new() { type = descriptorSetLayoutInfos[i].DescriptorType, descriptorCount = maxFramesInFlight, }; }

					fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes) {
						VkDescriptorPoolCreateInfo poolCreateInfo = new() { poolSizeCount = (uint)poolSizes.Length, pPoolSizes = poolSizesPtr, maxSets = maxFramesInFlight, };
						VkDescriptorPool descriptorPool;
						VkH.CheckIfSuccess(Vk.CreateDescriptorPool(logicalDevice, &poolCreateInfo, null, &descriptorPool), VulkanException.Reason.CreateDescriptorPool);
						return descriptorPool;
					}
				}

				[MustUseReturnValue]
				static VkDescriptorSetLayout CreateDescriptorSetLayout(VkDevice logicalDevice, DescriptorSetLayoutInfo[] descriptorSetLayoutInfos) {
					VkDescriptorSetLayoutBinding[] bindings = new VkDescriptorSetLayoutBinding[descriptorSetLayoutInfos.Length];
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
				static VkDescriptorSet[] CreateDescriptorSets(VkDevice logicalDevice, VkDescriptorPool descriptorPool, VkDescriptorSetLayout descriptorSetLayout, uint maxFramesInFlight, uint uniformBufferSize, T[] uniformBuffers,
					VkImageView imageView, VkSampler textureSampler) {
					VkDescriptorSetLayout[] layouts = new VkDescriptorSetLayout[maxFramesInFlight];
					for (int i = 0; i < layouts.Length; i++) { layouts[i] = descriptorSetLayout; }

					VkDescriptorSet[] descriptorSets = new VkDescriptorSet[maxFramesInFlight];
					fixed (VkDescriptorSetLayout* layoutsPtr = layouts) {
						fixed (VkDescriptorSet* descriptorSetsPtr = descriptorSets) {
							VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = new() { descriptorPool = descriptorPool, descriptorSetCount = (uint)descriptorSets.Length, pSetLayouts = layoutsPtr, };
							VkH.CheckIfSuccess(Vk.AllocateDescriptorSets(logicalDevice, &descriptorSetAllocateInfo, descriptorSetsPtr), VulkanException.Reason.AllocateDescriptorSets);
						}
					}

					for (int i = 0; i < maxFramesInFlight; i++) {
						VkDescriptorBufferInfo descriptorBufferInfo = new() { buffer = uniformBuffers[i].Buffer, offset = 0, range = uniformBufferSize, };
						VkDescriptorImageInfo descriptorImageInfo = new() { imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal, imageView = imageView, sampler = textureSampler, };

						VkWriteDescriptorSet writeDescriptorSet1 = new() {
								dstSet = descriptorSets[i], dstBinding = 0, dstArrayElement = 0, descriptorType = VkDescriptorType.DescriptorTypeUniformBuffer, descriptorCount = 1, pBufferInfo = &descriptorBufferInfo,
						};

						VkWriteDescriptorSet writeDescriptorSet2 = new() {
								dstSet = descriptorSets[i], dstBinding = 1, dstArrayElement = 0, descriptorType = VkDescriptorType.DescriptorTypeCombinedImageSampler, descriptorCount = 1, pImageInfo = &descriptorImageInfo,
						};

						VkWriteDescriptorSet[] writeDescriptorSets = [ writeDescriptorSet1, writeDescriptorSet2, ];

						fixed (VkWriteDescriptorSet* writeDescriptorSetsPtr = writeDescriptorSets) { Vk.UpdateDescriptorSets(logicalDevice, (uint)writeDescriptorSets.Length, writeDescriptorSetsPtr, 0, null); }
					}

					return descriptorSets;
				}
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

				VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new() { pushConstantRangeCount = 0, pPushConstantRanges = null, };
				if (this.descriptorSetLayout is { } descriptorSetLayout) {
					pipelineLayoutCreateInfo.setLayoutCount = 1;
					pipelineLayoutCreateInfo.pSetLayouts = &descriptorSetLayout;
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

								return new(debugName, logicalDevice, graphicsPipeline, pipelineLayout, descriptorPool, this.descriptorSetLayout, descriptorSets);
							}
						}
					}
				}
			}
		}

		public readonly record struct DescriptorSetLayoutInfo {
			public required VkDescriptorType DescriptorType { get; init; }
			public required VkShaderStageFlagBits StageFlags { get; init; }
			public required uint BindingLocation { get; init; }

			[SetsRequiredMembers]
			public DescriptorSetLayoutInfo(VkDescriptorType descriptorType, VkShaderStageFlagBits stageFlags, uint bindingLocation) {
				DescriptorType = descriptorType;
				StageFlags = stageFlags;
				BindingLocation = bindingLocation;
			}
		}
	}
}