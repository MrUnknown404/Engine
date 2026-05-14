using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Exceptions;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan.Objects;

public sealed unsafe class GraphicsPipeline : NamedGraphicsResource<GraphicsPipeline, ulong> {
	public VkPipeline Pipeline { get; }
	public VkPipelineLayout Layout { get; }

	protected override ulong Handle => Pipeline.Handle;

	private readonly LogicalGpu logicalGpu;

	internal GraphicsPipeline(SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu, Settings settings) : base(settings.DebugName) {
		Layout = CreateLayout(logicalGpu, settings);
		Pipeline = CreateGraphicsPipeline(physicalGpu, logicalGpu, settings, Layout);
		this.logicalGpu = logicalGpu;

		PrintCreate();
	}

	[MustUseReturnValue]
	private static VkPipelineLayout CreateLayout(LogicalGpu logicalGpu, Settings settings) {
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
				VkH.CheckIfSuccess(Vk.CreatePipelineLayout(logicalGpu.LogicalDevice, &pipelineLayoutCreateInfo, null, &tempPipelineLayout), VulkanException.Reason.CreatePipelineLayout);
				return tempPipelineLayout;
			}
		}
	}

	[MustUseReturnValue]
	private static VkPipeline CreateGraphicsPipeline(SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu, Settings settings, VkPipelineLayout pipelineLayout) {
		byte* entryPointName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference("main"u8));

		VkPipelineShaderStageCreateInfo* shaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[settings.Shaders.Length];
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
				rasterizerDiscardEnable = settings.EnableRasterizerDiscard ? VkH.True : VkH.False,
				polygonMode = settings.PolygonMode,
				cullMode = settings.CullMode,
				frontFace = settings.FrontFace,
				lineWidth = settings.LineWidth,
				depthClampEnable = settings.EnableDepthClamp ? VkH.True : VkH.False,
				depthBiasEnable = settings.EnableDepthBias ? VkH.True : VkH.False,
				depthBiasConstantFactor = settings.DepthBiasConstantFactor,
				depthBiasClamp = settings.DepthBiasClamp,
				depthBiasSlopeFactor = settings.DepthBiasSlopeFactor,
		};

		VkPipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new() {
				sampleShadingEnable = settings.EnableSampleShading ? VkH.True : VkH.False,
				rasterizationSamples = settings.RasterizationSamples,
				minSampleShading = settings.MinSampleShading,
				pSampleMask = null,
				alphaToCoverageEnable = settings.EnableAlphaToCoverage ? VkH.True : VkH.False,
				alphaToOneEnable = settings.EnableAlphaToOne ? VkH.True : VkH.False,
		};

		VkPipelineColorBlendAttachmentState colorBlendAttachmentState = new() {
				blendEnable = settings.EnableBlend ? VkH.True : VkH.False,
				colorWriteMask = settings.ColorComponentFlags,
				colorBlendOp = settings.ColorBlendOp,
				srcColorBlendFactor = settings.SrcColorBlendFactor,
				dstColorBlendFactor = settings.DstColorBlendFactor,
				alphaBlendOp = settings.AlphaBlendOp,
				srcAlphaBlendFactor = settings.SrcAlphaBlendFactor,
				dstAlphaBlendFactor = settings.DstAlphaBlendFactor,
		};

		VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new() { logicOpEnable = VkH.False, logicOp = VkLogicOp.LogicOpCopy, attachmentCount = 1, pAttachments = &colorBlendAttachmentState, };
		// colorBlendStateCreateInfo.blendConstants[0] = 0; // is there a better way of initializing this?
		// colorBlendStateCreateInfo.blendConstants[1] = 0;
		// colorBlendStateCreateInfo.blendConstants[2] = 0;
		// colorBlendStateCreateInfo.blendConstants[3] = 0;

		VkPipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new() {
				depthTestEnable = settings.EnableDepthTest ? VkH.True : VkH.False,
				depthWriteEnable = settings.EnableDepthWrite ? VkH.True : VkH.False,
				depthCompareOp = settings.DepthCompareOp,
				depthBoundsTestEnable = settings.EnableDepthBoundsTest ? VkH.True : VkH.False,
				minDepthBounds = settings.MinDepthBounds,
				maxDepthBounds = settings.MaxDepthBounds,
				stencilTestEnable = settings.EnableStencilTest ? VkH.True : VkH.False,
				front = settings.StencilFront,
				back = settings.StencilBack,
		};

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
							stageCount = (uint)settings.Shaders.Length,
							pStages = shaderStageCreateInfos,
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
					VkH.CheckIfSuccess(Vk.CreateGraphicsPipelines(logicalGpu.LogicalDevice, VkPipelineCache.Zero, 1, &pipelineCreateInfo, null, &graphicsPipeline), VulkanException.Reason.CreateGraphicsPipeline);
					return graphicsPipeline;
				}
			}
		}
	}

	protected override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.LogicalDevice;

		Vk.DestroyPipelineLayout(logicalDevice, Layout, null);
		Vk.DestroyPipeline(logicalDevice, Pipeline, null);
	}

	[PublicAPI]
	public class Settings {
		public string DebugName { get; }

		// Pipeline Input
		public VkPrimitiveTopology Topology { get; init; } = VkPrimitiveTopology.PrimitiveTopologyTriangleList;
		public VkVertexInputAttributeDescription[] VertexAttributeDescriptions { get; }
		public VkVertexInputBindingDescription[] VertexBindingDescriptions { get; }
		public VulkanShader[] Shaders { get; }
		public VkFormat SwapChainImageFormat { get; }

		// Rasterization
		public bool EnableRasterizerDiscard { get; init; }
		public VkPolygonMode PolygonMode { get; init; } = VkPolygonMode.PolygonModeFill;
		public VkCullModeFlagBits CullMode { get; init; } = VkCullModeFlagBits.CullModeBackBit;
		public VkFrontFace FrontFace { get; init; } = VkFrontFace.FrontFaceCounterClockwise;
		public float LineWidth { get; init; } = 1;
		public bool EnableDepthClamp { get; init; }
		public bool EnableDepthBias { get; init; }
		public float DepthBiasConstantFactor { get; init; }
		public float DepthBiasClamp { get; init; }
		public float DepthBiasSlopeFactor { get; init; }

		// Multisampling
		public bool EnableSampleShading { get; init; }
		public VkSampleCountFlagBits RasterizationSamples { get; init; } = VkSampleCountFlagBits.SampleCount1Bit;
		public float MinSampleShading { get; init; } = 1;
		public bool EnableAlphaToCoverage { get; init; }
		public bool EnableAlphaToOne { get; init; }

		// Color Blend
		public bool EnableBlend { get; init; } = true;
		public VkColorComponentFlagBits ColorComponentFlags { get; init; } = VkColorComponentFlagBits.ColorComponentRBit |
																			 VkColorComponentFlagBits.ColorComponentGBit |
																			 VkColorComponentFlagBits.ColorComponentBBit |
																			 VkColorComponentFlagBits.ColorComponentABit;
		public VkBlendOp ColorBlendOp { get; init; } = VkBlendOp.BlendOpAdd;
		public VkBlendFactor SrcColorBlendFactor { get; init; } = VkBlendFactor.BlendFactorSrcAlpha;
		public VkBlendFactor DstColorBlendFactor { get; init; } = VkBlendFactor.BlendFactorOneMinusSrcAlpha;
		public VkBlendOp AlphaBlendOp { get; init; } = VkBlendOp.BlendOpAdd;
		public VkBlendFactor SrcAlphaBlendFactor { get; init; } = VkBlendFactor.BlendFactorOne;
		public VkBlendFactor DstAlphaBlendFactor { get; init; } = VkBlendFactor.BlendFactorZero;

		// Depth Stencil
		public bool EnableDepthTest { get; init; }
		public bool EnableDepthWrite { get; init; }
		public VkCompareOp DepthCompareOp { get; init; } = VkCompareOp.CompareOpLess;
		public bool EnableDepthBoundsTest { get; init; }
		public float MinDepthBounds { get; init; }
		public float MaxDepthBounds { get; init; } = 1;
		public bool EnableStencilTest { get; init; }
		public VkStencilOpState StencilFront { get; init; }
		public VkStencilOpState StencilBack { get; init; }

		// Dynamic State
		public VkDynamicState[] DynamicStates { get; init; } = [ VkDynamicState.DynamicStateViewport, VkDynamicState.DynamicStateScissor, ];

		// Resources
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