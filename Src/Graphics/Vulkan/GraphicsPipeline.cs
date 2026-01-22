using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Graphics.Vulkan {
	public class GraphicsPipeline {
		public VkPipeline Pipeline { get; }
		public VkPipelineLayout Layout { get; }

		public VkDescriptorSetLayout? DescriptorSetLayout { get; }
		public VkDescriptorPool? DescriptorPool { get; }
		public VkDescriptorSet[]? DescriptorSets { get; }

		private readonly VkDevice logicalDevice;

		private GraphicsPipeline(VkDevice logicalDevice, VkPipeline pipeline, VkPipelineLayout layout, VkDescriptorSetLayout? descriptorSetLayout, VkDescriptorPool? descriptorPool, VkDescriptorSet[]? descriptorSets) {
			this.logicalDevice = logicalDevice;
			Pipeline = pipeline;
			Layout = layout;
			DescriptorSetLayout = descriptorSetLayout;
			DescriptorPool = descriptorPool;
			DescriptorSets = descriptorSets;
		}

		public void CmdBind(VkCommandBuffer graphicsCommandBuffer) => Vk.CmdBindPipeline(graphicsCommandBuffer, VkPipelineBindPoint.PipelineBindPointGraphics, Pipeline);

		public unsafe void Cleanup() {
			if (DescriptorPool is { } descriptorPool) { Vk.DestroyDescriptorPool(logicalDevice, descriptorPool, null); }
			if (DescriptorSetLayout is { } descriptorSetLayout) { Vk.DestroyDescriptorSetLayout(logicalDevice, descriptorSetLayout, null); }

			Vk.DestroyPipelineLayout(logicalDevice, Layout, null);
			Vk.DestroyPipeline(logicalDevice, Pipeline, null);
		}

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
			public VkDynamicState[] DynamicStates { get; init; } = [ VkDynamicState.DynamicStateViewport, VkDynamicState.DynamicStateScissor, ];

			private VkDescriptorSetLayout? descriptorSetLayout;
			private VkDescriptorPool? descriptorPool;
			private VkDescriptorSet[]? descriptorSets;

			private readonly VkDevice logicalDevice;
			private readonly SwapChain swapChain;
			private readonly ShaderStageInfo[] shaderStageInfos;
			private readonly VkVertexInputAttributeDescription[] vertexAttributeDescriptions;
			private readonly VkVertexInputBindingDescription[] vertexBindingDescriptions;

			public Builder(VkDevice logicalDevice, SwapChain swapChain, ShaderStageInfo[] shaderStageInfos, VkVertexInputAttributeDescription[] vertexAttributeDescriptions,
				VkVertexInputBindingDescription[] vertexBindingDescriptions) {
				this.logicalDevice = logicalDevice;
				this.swapChain = swapChain;
				this.shaderStageInfos = shaderStageInfos;
				this.vertexAttributeDescriptions = vertexAttributeDescriptions;
				this.vertexBindingDescriptions = vertexBindingDescriptions;
			}

			public void AddDescriptorSets(VkShaderStageFlagBits shaderStageFlags, uint bindingLocation, uint maxFramesInFlight, VkBuffer[] uniformBuffers, uint uniformBufferSize) {
				descriptorPool = VkH.CreateDescriptorPool(logicalDevice, VkDescriptorType.DescriptorTypeUniformBuffer, maxFramesInFlight);
				descriptorSetLayout = VkH.CreateDescriptorSetLayout(logicalDevice, bindingLocation, shaderStageFlags);
				descriptorSets = VkH.CreateDescriptorSets(logicalDevice, descriptorPool.Value, descriptorSetLayout.Value, maxFramesInFlight, uniformBufferSize, uniformBuffers);
			}

			public unsafe GraphicsPipeline MakePipeline() {
				byte* entryPointName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("main"u8));
				VkPipelineShaderStageCreateInfo[] shaderStageCreateInfos = new VkPipelineShaderStageCreateInfo[shaderStageInfos.Length];
				for (int i = 0; i < shaderStageInfos.Length; i++) {
					ShaderStageInfo shaderStageInfo = shaderStageInfos[i];
					shaderStageCreateInfos[i] = new() { module = shaderStageInfo.ShaderModule, stage = shaderStageInfo.ShaderStageFlags, pName = entryPointName, };
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
				if (Vk.CreatePipelineLayout(logicalDevice, &pipelineLayoutCreateInfo, null, &pipelineLayout) != VkResult.Success) { throw new VulkanException("Failed to create pipeline layout"); }

				fixed (VkPipelineShaderStageCreateInfo* shaderStageCreateInfosPtr = shaderStageCreateInfos) {
					fixed (VkDynamicState* dynamicStatesPtr = DynamicStates) {
						fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = vertexAttributeDescriptions) {
							fixed (VkVertexInputBindingDescription* vertexBindingDescriptionPtr = vertexBindingDescriptions) {
								VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new() { dynamicStateCount = (uint)DynamicStates.Length, pDynamicStates = dynamicStatesPtr, };

								VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new() {
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
								VkResult result = Vk.CreateGraphicsPipelines(logicalDevice, VkPipelineCache.Zero, 1, &pipelineCreateInfo, null, &graphicsPipeline);

								return result != VkResult.Success ?
										throw new VulkanException($"Failed to create graphics pipeline. {result}") :
										new(logicalDevice, graphicsPipeline, pipelineLayout, this.descriptorSetLayout, descriptorPool, descriptorSets);
							}
						}
					}
				}
			}
		}
	}
}