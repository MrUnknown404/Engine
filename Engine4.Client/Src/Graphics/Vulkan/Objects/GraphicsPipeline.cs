using Engine4.Client.Utility;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Client.Graphics.Vulkan.Objects;

public sealed unsafe class GraphicsPipeline : NamedVulkanResource {
	public VkPipeline Pipeline { get; }
	public override ulong Handle => Pipeline.Handle;

	[Obsolete]
	public VkPipelineLayout Layout { get; }

	private readonly LogicalGpu logicalGpu;

	internal GraphicsPipeline(SurfaceReadyPhysicalGpu physicalGpu, LogicalGpu logicalGpu, Settings settings) : base(settings.DebugName) {
		this.logicalGpu = logicalGpu;
		Layout = CreateLayout(logicalGpu, settings);
		Pipeline = CreatePipeline(logicalGpu, settings, Layout, physicalGpu.DepthFormat);
	}

	protected internal override void Cleanup() {
		VkDevice logicalDevice = logicalGpu.VkLogicalDevice;

		Vk.DestroyPipelineLayout(logicalDevice, Layout, null);
		Vk.DestroyPipeline(logicalDevice, Pipeline, null);
	}

	[MustUseReturnValue]
	private static VkPipelineLayout CreateLayout(LogicalGpu logicalGpu, Settings settings) {
		VkDescriptorSetLayout[]? descriptorSetLayouts = settings.DescriptorSetLayouts;
		VkPushConstantRange[]? pushConstantRanges = settings.PushConstantRanges;

		fixed (VkDescriptorSetLayout* pDescriptorSetLayout = descriptorSetLayouts) {
			fixed (VkPushConstantRange* pPushConstantRange = pushConstantRanges) {
				VkPipelineLayoutCreateInfo layoutCreateInfo = new() { /* flags = , any flags? */ };

				if (descriptorSetLayouts != null) { layoutCreateInfo = layoutCreateInfo with { setLayoutCount = (uint)descriptorSetLayouts.Length, pSetLayouts = pDescriptorSetLayout, }; }
				if (pushConstantRanges != null) { layoutCreateInfo = layoutCreateInfo with { pushConstantRangeCount = (uint)pushConstantRanges.Length, pPushConstantRanges = pPushConstantRange, }; }

				VkPipelineLayout layout;
				VkH.ThrowIfNotSuccess(Vk.CreatePipelineLayout(logicalGpu.VkLogicalDevice, &layoutCreateInfo, null, &layout), "Failed to create pipeline layout");
				return layout;
			}
		}
	}

	// TODO see if i can clean this up a little bit
	[MustUseReturnValue]
	private static VkPipeline CreatePipeline(LogicalGpu logicalGpu, Settings settings, VkPipelineLayout pipelineLayout, VkFormat? depthFormat) {
		using StringUtf8Ptr entryPoint = new(Shader.ShaderEntryPoint);

		VkPipelineShaderStageCreateInfo* pShaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[settings.Shaders.Length];
		for (int i = 0; i < settings.Shaders.Length; i++) {
			Shader shader = settings.Shaders[i];

			VkPipelineShaderStageCreateInfo shaderStageCreateInfo = new() {
					// flags = , any flags?
					module = shader.ShaderModule, //
					stage = shader.ShaderType.ShaderStageFlags,
					pName = entryPoint,
			};

			if (shader.SpecializationInfo is { } specializationInfo) { shaderStageCreateInfo = shaderStageCreateInfo with { pSpecializationInfo = &specializationInfo, }; }

			pShaderStageCreateInfos[i] = shaderStageCreateInfo;
		}

		VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new() {
				topology = settings.Topology, //
		};

		VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new() {
				viewportCount = 1, //
				scissorCount = 1,
		};

		VkFormat swapChainImageFormat = settings.SwapChainImageFormat;
		VkPipelineRenderingCreateInfo renderingCreateInfo = new() {
				colorAttachmentCount = 1, //
				pColorAttachmentFormats = &swapChainImageFormat,
				depthAttachmentFormat = depthFormat ?? default,
		};

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

		// float* blendConstants = stackalloc float[4] { 0, 0, 0, 0, }; // TODO do i want this?
		VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new() {
				logicOpEnable = VkH.False, //
				logicOp = VkLogicOp.LogicOpCopy,
				attachmentCount = 1,
				pAttachments = &colorBlendAttachmentState,
		};

		// i'm not sure if this should be created if we aren't using depth testing? idk
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

		fixed (VkDynamicState* pDynamicStates = settings.DynamicStates) {
			fixed (VkVertexInputAttributeDescription* pAttributeDescriptions = settings.VertexAttributeDescriptions) {
				fixed (VkVertexInputBindingDescription* pVertexBindingDescription = settings.VertexBindingDescriptions) {
					VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new() {
							dynamicStateCount = (uint)settings.DynamicStates.Length, //
							pDynamicStates = pDynamicStates,
					};

					VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo = new() {
							vertexBindingDescriptionCount = (uint)settings.VertexBindingDescriptions.Length,
							pVertexBindingDescriptions = pVertexBindingDescription,
							vertexAttributeDescriptionCount = (uint)settings.VertexAttributeDescriptions.Length,
							pVertexAttributeDescriptions = pAttributeDescriptions,
					};

					VkGraphicsPipelineCreateInfo graphicsPipelineCreateInfo = new() {
							pNext = &renderingCreateInfo,
							stageCount = (uint)settings.Shaders.Length,
							pStages = pShaderStageCreateInfos,
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
							// flags = , if this is needed use VkPipelineCreateFlagBits2 instead
					};

					VkPipeline graphicsPipeline;
					VkH.ThrowIfNotSuccess(Vk.CreateGraphicsPipelines(logicalGpu.VkLogicalDevice, VkPipelineCache.Zero, 1, &graphicsPipelineCreateInfo, null, &graphicsPipeline), "Failed to create graphics pipeline");
					return graphicsPipeline;
				}
			}
		}
	}

	public sealed class Settings {
		public string DebugName { get; }

		// Pipeline Input
		public VkPrimitiveTopology Topology { get; init; } = VkPrimitiveTopology.PrimitiveTopologyTriangleList;
		public VkVertexInputAttributeDescription[] VertexAttributeDescriptions { get; }
		public VkVertexInputBindingDescription[] VertexBindingDescriptions { get; }
		public Shader[] Shaders { get; }
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
		public float MinDepthBounds { get; init; } = 0;
		public float MaxDepthBounds { get; init; } = 1;
		public bool EnableStencilTest { get; init; }
		public VkStencilOpState StencilFront { get; init; }
		public VkStencilOpState StencilBack { get; init; }

		// Dynamic State
		public VkDynamicState[] DynamicStates { get; init; } = [ VkDynamicState.DynamicStateViewport, VkDynamicState.DynamicStateScissor, ];

		// Resources
		[Obsolete("Add support for the new descriptor heaps")]
		public VkDescriptorSetLayout[]? DescriptorSetLayouts { get; init; } // TODO look into the new Descriptor Heaps
		public VkPushConstantRange[]? PushConstantRanges { get; init; }

		public Settings(string debugName, VkFormat swapChainImageFormat, Shader[] shaders, VkVertexInputAttributeDescription[] vertexAttributeDescriptions,
			VkVertexInputBindingDescription[] vertexBindingDescriptions) {
			DebugName = debugName;
			SwapChainImageFormat = swapChainImageFormat;
			Shaders = shaders;
			VertexAttributeDescriptions = vertexAttributeDescriptions;
			VertexBindingDescriptions = vertexBindingDescriptions;
		}
	}
}