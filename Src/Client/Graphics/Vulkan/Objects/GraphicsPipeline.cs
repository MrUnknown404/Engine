using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects {
	public sealed unsafe class GraphicsPipeline : NamedGraphicsResource<GraphicsPipeline, ulong> {
		public VkPipeline Pipeline { get; }
		public VkPipelineLayout Layout { get; }

		protected override ulong Handle => Pipeline.Handle;

		private readonly VkDevice logicalDevice;

		internal GraphicsPipeline(SurfaceCapablePhysicalGpu physicalGpu, VkDevice logicalDevice, Settings settings) : base(settings.DebugName) {
			Pipeline = CreateGraphicsPipeline(physicalGpu, logicalDevice, settings, out VkPipelineLayout layout);
			Layout = layout;
			this.logicalDevice = logicalDevice;

			PrintCreate();
		}

		[MustUseReturnValue]
		private static VkPipeline CreateGraphicsPipeline(SurfaceCapablePhysicalGpu physicalGpu, VkDevice logicalDevice, Settings settings, out VkPipelineLayout pipelineLayout) {
			byte* entryPointName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("main"u8));

			fixed (VkDescriptorSetLayout* descriptorSetLayoutsPtr = settings.DescriptorSetLayouts) {
				fixed (VkPushConstantRange* pushConstantRangesPtr = settings.PushConstantRanges) {
					VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new();

					if (settings.DescriptorSetLayouts != null) {
						pipelineLayoutCreateInfo.setLayoutCount = (uint)settings.DescriptorSetLayouts.Length;
						pipelineLayoutCreateInfo.pSetLayouts = descriptorSetLayoutsPtr;
					}

					if (settings.PushConstantRanges != null) {
						pipelineLayoutCreateInfo.pushConstantRangeCount = (uint)settings.PushConstantRanges.Length;
						pipelineLayoutCreateInfo.pPushConstantRanges = pushConstantRangesPtr;
					}

					VkPipelineLayout tempPipelineLayout;
					VkH.CheckIfSuccess(Vk.CreatePipelineLayout(logicalDevice, &pipelineLayoutCreateInfo, null, &tempPipelineLayout), VulkanException.Reason.CreatePipelineLayout);
					pipelineLayout = tempPipelineLayout;
				}
			}

			VkPipelineShaderStageCreateInfo[] shaderStageCreateInfos = new VkPipelineShaderStageCreateInfo[settings.Shaders.Length];
			for (int i = 0; i < settings.Shaders.Length; i++) {
				VulkanShader shader = settings.Shaders[i];

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

				if (shader.SpecializationInfo is { } specializationInfo) { shaderStageCreateInfos[i].pSpecializationInfo = &specializationInfo; }
			}

			VkFormat swapChainImageFormat = settings.SwapChainImageFormat;
			VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new() { topology = settings.Topology, };
			VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new() { viewportCount = 1, scissorCount = 1, };
			VkPipelineRenderingCreateInfo renderingCreateInfo = new() { colorAttachmentCount = 1, pColorAttachmentFormats = &swapChainImageFormat, depthAttachmentFormat = physicalGpu.FindDepthFormat(), };

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

			VkPipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new() {
					depthTestEnable = (int)(settings.EnableDepthTest ? Vk.True : Vk.False),
					depthWriteEnable = (int)(settings.EnableDepthTest ? Vk.True : Vk.False),
					depthCompareOp = settings.DepthCompareOp,
					depthBoundsTestEnable = (int)Vk.False,
					minDepthBounds = 0,
					maxDepthBounds = 1,
					stencilTestEnable = (int)Vk.False,
			};

			fixed (VkPipelineShaderStageCreateInfo* shaderStageCreateInfosPtr = shaderStageCreateInfos) {
				fixed (VkDynamicState* dynamicStatesPtr = settings.DynamicStates) {
					fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = settings.VertexAttributeDescriptions) {
						fixed (VkVertexInputBindingDescription* vertexBindingDescriptionPtr = settings.VertexBindingDescriptions) {
							VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new() { dynamicStateCount = (uint)settings.DynamicStates.Length, pDynamicStates = dynamicStatesPtr, };

							VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new() {
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
									pDepthStencilState = &depthStencilStateCreateInfo,
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
		}

		protected override void Cleanup() {
			Vk.DestroyPipelineLayout(logicalDevice, Layout, null);
			Vk.DestroyPipeline(logicalDevice, Pipeline, null);
		}

		public class Settings {
			public string DebugName { get; }
			public VkFormat SwapChainImageFormat { get; }
			public VulkanShader[] Shaders { get; }
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
			public bool EnableDepthTest { get; init; } = true;
			public VkCompareOp DepthCompareOp { get; init; } = VkCompareOp.CompareOpLess;

			public VkDescriptorSetLayout[]? DescriptorSetLayouts { get; init; }
			public VkPushConstantRange[]? PushConstantRanges { get; init; }

			public Settings(string debugName, VkFormat swapChainImageFormat, VulkanShader[] shaders, VkVertexInputAttributeDescription[] vertexAttributeDescriptions, VkVertexInputBindingDescription[] vertexBindingDescriptions) {
				DebugName = debugName;
				SwapChainImageFormat = swapChainImageFormat;
				Shaders = shaders;
				VertexAttributeDescriptions = vertexAttributeDescriptions;
				VertexBindingDescriptions = vertexBindingDescriptions;
			}
		}
	}
}