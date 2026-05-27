using System.Numerics;
using System.Runtime.InteropServices;
using Engine3.Client.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Client.ImGui;
using ImGuiNET;
using JetBrains.Annotations;
using OpenTK.Graphics.Vulkan;
using ImGuiNet = ImGuiNET.ImGui;

namespace Engine3.Client.Client.Graphics.Vulkan.Renderers;

public unsafe class VulkanImGuiRenderer : ImGuiRenderer {
	public ImGuiFragmentShaderConstants ImGuiShaderConstants { get; init; } = new(true);

	protected sealed override VulkanResourceProvider GraphicsResourceProvider { get; }

	private readonly GraphicsPipeline graphicsPipeline;

	private readonly DescriptorSets samplerDescriptorSets;
	private readonly DescriptorSetLayout imageDescriptorSetsLayout;

	private VulkanBuffer vertexBuffer; // TODO Frames-in-flight? i think i read somewhere i should? look into
	private VulkanBuffer indexBuffer; // ^

	private GraphicsCommandBuffer graphicsCommandBuffer = null!;
	private byte frameIndex;

	private readonly Dictionary<nint, DescriptorSets> imageDescriptors = new();

	public VulkanImGuiRenderer(ImGuiBackend imGuiBackend, VulkanRendererBase renderer) : base(imGuiBackend) {
		GraphicsResourceProvider = renderer.GraphicsResourceProvider;

		CreatePipeline(GraphicsResourceProvider, renderer.SwapChain.ImageFormat, renderer.MaxFramesInFlight, ImGuiShaderConstants, out graphicsPipeline, out samplerDescriptorSets, out imageDescriptorSetsLayout);
		CreateBuffers(GraphicsResourceProvider, out vertexBuffer, out indexBuffer);
		CreateFont(imGuiBackend, GraphicsResourceProvider, renderer.PhysicalGpu, renderer.LogicalGpu, renderer.TransferCommandPool, out VulkanImage fontImage, out TextureSampler textureSampler);

		samplerDescriptorSets.UpdateDescriptorSet(0, textureSampler.Sampler);
		_ = AddTexture(fontImage, renderer.MaxFramesInFlight);
	}

	[MustUseReturnValue]
	public nint AddTexture(VulkanImage image, byte maxFramesInFlight) { // TODO add remove
		DescriptorPool descriptorPool = GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeSampledImage, ], 1, maxFramesInFlight); // TODO use existing pool
		DescriptorSets descriptorSets = descriptorPool.AllocateDescriptorSets(imageDescriptorSetsLayout);
		descriptorSets.UpdateDescriptorSet(0, image.ImageView);

		nint id = imageDescriptors.Count;
		imageDescriptors[id] = descriptorSets;
		return id;
	}

	internal void SetFrameData(GraphicsCommandBuffer graphicsCommandBuffer, byte frameIndex) {
		this.graphicsCommandBuffer = graphicsCommandBuffer;
		this.frameIndex = frameIndex;
	}

	protected internal override void CopyBuffers(ImDrawDataPtr drawData) {
		if (drawData.TotalVtxCount > (uint)(vertexBuffer.BufferSize / (uint)sizeof(ImDrawVert))) {
			GraphicsResourceProvider.EnqueueDestroy(vertexBuffer);

			vertexBuffer = GraphicsResourceProvider.CreateBuffer(vertexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, (ulong)(drawData.TotalVtxCount * sizeof(ImDrawVert)));
		}

		if (drawData.TotalIdxCount > (uint)(indexBuffer.BufferSize / sizeof(ushort))) {
			GraphicsResourceProvider.EnqueueDestroy(indexBuffer);

			indexBuffer = GraphicsResourceProvider.CreateBuffer(indexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, (ulong)(drawData.TotalIdxCount * sizeof(ushort)));
		}

		// do i copy just when the data is different? i feel like i should but i don't know how to check that easily

		ImDrawVert* vertexBufferMap = (ImDrawVert*)vertexBuffer.MapMemory(vertexBuffer.BufferSize);
		ushort* indexBufferMap = (ushort*)indexBuffer.MapMemory(indexBuffer.BufferSize);

		for (int i = 0; i < drawData.CmdListsCount; i++) {
			ImDrawListPtr drawList = drawData.CmdLists[i];

			Buffer.MemoryCopy((void*)drawList.VtxBuffer.Data, vertexBufferMap, vertexBuffer.BufferSize, (ulong)(drawList.VtxBuffer.Size * sizeof(ImDrawVert)));
			Buffer.MemoryCopy((void*)drawList.IdxBuffer.Data, indexBufferMap, indexBuffer.BufferSize, (ulong)(drawList.IdxBuffer.Size * sizeof(ushort)));

			vertexBufferMap += drawList.VtxBuffer.Size;
			indexBufferMap += drawList.IdxBuffer.Size;
		}

		vertexBuffer.UnmapMemory();
		indexBuffer.UnmapMemory();
	}

	protected internal override void DrawFrame(ImDrawDataPtr drawData) {
		graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

		graphicsCommandBuffer.CmdSetViewport(0, 0, (uint)drawData.DisplaySize.X, (uint)drawData.DisplaySize.Y, 0, 1);

		graphicsCommandBuffer.CmdPushConstants(graphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit, new ImGuiPushConstants(new(-1), new(2f / drawData.DisplaySize.X, 2f / drawData.DisplaySize.Y)));
		graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, samplerDescriptorSets.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageFragmentBit);

		graphicsCommandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
		graphicsCommandBuffer.CmdBindIndexBuffer(indexBuffer, indexBuffer.BufferSize, VkIndexType.IndexTypeUint16);

		Vector2 clipOff = drawData.DisplayPos;
		VkDescriptorSet lastTexture = VkDescriptorSet.Zero;

		int vertexOffset = 0;
		uint indexOffset = 0;

		for (int i = 0; i < drawData.CmdListsCount; i++) {
			ImDrawListPtr cmdList = drawData.CmdLists[i];

			for (int j = 0; j < cmdList.CmdBuffer.Size; j++) {
				ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[j];

				Vector2 clipMin = new(drawCmd.ClipRect.X - clipOff.X, drawCmd.ClipRect.Y - clipOff.Y);
				Vector2 clipMax = new(drawCmd.ClipRect.Z - clipOff.X, drawCmd.ClipRect.W - clipOff.Y);

				if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y) { continue; }

				graphicsCommandBuffer.CmdSetScissor((int)clipMin.X, (int)clipMin.Y, (uint)(clipMax.X - clipMin.X), (uint)(clipMax.Y - clipMin.Y));

				VkDescriptorSet texture = imageDescriptors[drawCmd.TextureId].GetCurrent(frameIndex);
				if (texture != lastTexture) { graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, texture, VkShaderStageFlagBits.ShaderStageFragmentBit, 1); }
				lastTexture = texture;

				graphicsCommandBuffer.CmdDrawIndexed(drawCmd.ElemCount, 1, indexOffset, vertexOffset, 0);

				indexOffset += drawCmd.ElemCount;
			}

			vertexOffset += cmdList.VtxBuffer.Size;
		}
	}

	private static void CreatePipeline(VulkanResourceProvider graphicsResourceProvider, VkFormat swapFormatImageFormat, byte maxFramesInFlight, ImGuiFragmentShaderConstants imGuiShaderConstants,
		out GraphicsPipeline graphicsPipeline, out DescriptorSets samplerDescriptorSet, out DescriptorSetLayout imageDescriptorSetLayout) {
		ImGuiFragmentShaderConstants shaderConstants = imGuiShaderConstants;
		VkSpecializationMapEntry specializationMapEntry = new() { constantID = 0, offset = 0, size = sizeof(uint), };
		VkSpecializationInfo specializationInfo = new() { dataSize = (nuint)sizeof(ImGuiFragmentShaderConstants), mapEntryCount = 1, pMapEntries = &specializationMapEntry, pData = &shaderConstants, };

		VulkanShader vertexShader = graphicsResourceProvider.CreateShader($"{ImGuiAssetName} Vertex Shader", ImGuiAssetName, ShaderLanguage.Glsl, ShaderType.Vertex, Core.Engine3.Engine.Assembly, specializationInfo);
		VulkanShader fragmentShader = graphicsResourceProvider.CreateShader($"{ImGuiAssetName} Fragment Shader", ImGuiAssetName, ShaderLanguage.Glsl, ShaderType.Fragment, Core.Engine3.Engine.Assembly);

		DescriptorSetLayout samplerDescriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);
		imageDescriptorSetLayout = graphicsResourceProvider.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeSampledImage, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);

		DescriptorPool descriptorPool = graphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeSampler, ], 1, maxFramesInFlight);

		samplerDescriptorSet = descriptorPool.AllocateDescriptorSets(samplerDescriptorSetLayout);

		VkVertexInputAttributeDescription[] vertexAttributeDescriptions = [
				new() { binding = 0, location = 0, format = VkFormat.FormatR32g32Sfloat, offset = 0, }, //
				new() { binding = 0, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 2, }, //
				new() { binding = 0, location = 2, format = VkFormat.FormatR8g8b8a8Unorm, offset = sizeof(float) * 4, },
		];

		VkVertexInputBindingDescription[] vertexBindingDescriptions = [ new() { binding = 0, stride = (uint)sizeof(ImDrawVert), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

		graphicsPipeline = graphicsResourceProvider.CreateGraphicsPipeline(
			new($"{ImGuiAssetName} Graphics Pipeline", swapFormatImageFormat, [ vertexShader, fragmentShader, ], vertexAttributeDescriptions, vertexBindingDescriptions) {
					DescriptorSetLayouts = [ samplerDescriptorSetLayout.VkDescriptorSetLayout, imageDescriptorSetLayout.VkDescriptorSetLayout, ],
					PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, offset = 0, size = (uint)sizeof(ImGuiPushConstants), }, ],
					FrontFace = VkFrontFace.FrontFaceClockwise, // ??
					CullMode = VkCullModeFlagBits.CullModeNone, // TODO getting weird artifacts without this set. figure out why and what i should be using
					SrcAlphaBlendFactor = VkBlendFactor.BlendFactorOneMinusSrcAlpha,
			});

		graphicsResourceProvider.EnqueueDestroy(vertexShader);
		graphicsResourceProvider.EnqueueDestroy(fragmentShader);
	}

	private static void CreateBuffers(VulkanResourceProvider graphicsResourceProvider, out VulkanBuffer vertexBuffer, out VulkanBuffer indexBuffer) {
		vertexBuffer = graphicsResourceProvider.CreateBuffer($"{ImGuiAssetName} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, 1);

		indexBuffer = graphicsResourceProvider.CreateBuffer($"{ImGuiAssetName} Index Buffer", VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, 1);
	}

	private static void CreateFont(ImGuiBackend imGuiBackend, VulkanResourceProvider graphicsResourceProvider, SurfaceCapablePhysicalGpu physicalGpu, LogicalGpu logicalGpu, TransferCommandPool transferCommandPool,
		out VulkanImage fontImage, out TextureSampler textureSampler) {
		ImGuiNet.SetCurrentContext(imGuiBackend.Context);
		ImGuiIOPtr io = ImGuiNet.GetIO();

		io.Fonts.GetTexDataAsRGBA32(out byte* fontData, out int fontImageWidth, out int fontImageHeight, out int texChannels);

		fontImage = graphicsResourceProvider.CreateImage($"{ImGuiAssetName} Font Image", (uint)fontImageWidth, (uint)fontImageHeight, VkFormat.FormatR8g8b8a8Unorm);
		transferCommandPool.CopyToImage(fontImage, physicalGpu.QueueFamilyIndices, logicalGpu.TransferQueue, (uint)fontImageWidth, (uint)fontImageHeight, (byte)texChannels, fontData);

		io.Fonts.ClearTexData(); // do i need to call this?

		textureSampler = graphicsResourceProvider.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, physicalGpu.PhysicalDeviceProperties2.properties.limits) {
				AddressMode = new(VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge),
				BorderColor = VkBorderColor.BorderColorFloatOpaqueWhite,
		});
	}

	[StructLayout(LayoutKind.Explicit, Size = 4)] // i think this needs to be aligned to 4 bytes
	public readonly record struct ImGuiFragmentShaderConstants {
		[field: FieldOffset(0)]
		public bool UseFastLinearColorConversion { get; init; }

		public ImGuiFragmentShaderConstants(bool useFastLinearColorConversion) => UseFastLinearColorConversion = useFastLinearColorConversion;
	}

	private readonly record struct ImGuiPushConstants {
		public Vector2 Translate { get; init; }
		public Vector2 Scale { get; init; }

		public ImGuiPushConstants(Vector2 translate, Vector2 scale) {
			Translate = translate;
			Scale = scale;
		}
	}
}