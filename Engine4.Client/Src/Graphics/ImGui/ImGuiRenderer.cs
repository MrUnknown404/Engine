using System.Reflection;
using System.Runtime.InteropServices;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using Hexa.NET.ImGui;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine4.Client.Graphics.ImGui;

public unsafe class ImGuiRenderer { // TODO
	private const string ImGuiAssetName = "ImGui";

	// private readonly VulkanResourceManager resourceManager;
	//
	// private readonly GraphicsPipeline graphicsPipeline;
	//
	// private readonly DescriptorSets samplerDescriptorSets;
	// private readonly DescriptorSetLayout imageDescriptorSetsLayout;
	//
	// private readonly VulkanBuffer vertexBuffer; // TODO Frames-in-flight? i think i read somewhere i should? look into
	// private readonly VulkanBuffer indexBuffer; // ^
	//
	// private readonly Dictionary<nint, DescriptorSets> imageDescriptors = new();

	public ImGuiRenderer(VulkanResourceManager resourceManager, VkFormat swapChainImageFormat, byte maxFramesInFlight, ImGuiFragmentShaderConstants shaderConstants) {
		// this.resourceManager = resourceManager;
		//
		// CreatePipeline(resourceManager, swapChainImageFormat, maxFramesInFlight, shaderConstants, out graphicsPipeline, out samplerDescriptorSets, out imageDescriptorSetsLayout);
		// CreateBuffers(resourceManager, out vertexBuffer, out indexBuffer);
		// CreateFont(resourceManager, out VulkanTexture fontImage, out TextureSampler textureSampler);

		// samplerDescriptorSets.UpdateDescriptorSet(0, textureSampler.Sampler);
		// _ = AddTexture(fontImage, maxFramesInFlight);
	}

	internal void CopyBuffers(ImDrawDataPtr imDrawData, float delta) {
		// if (imDrawData.TotalVtxCount > (uint)(vertexBuffer.Size / (uint)sizeof(ImDrawVert))) { vertexBuffer.Resize((ulong)(imDrawData.TotalVtxCount * sizeof(ImDrawVert))); }
		// if (imDrawData.TotalIdxCount > (uint)(indexBuffer.Size / sizeof(ushort))) { indexBuffer.Resize((ulong)(imDrawData.TotalIdxCount * sizeof(ushort))); }
		//
		// // do i copy just when the data is different? i feel like i should but i don't know how to check that easily
		//
		// ImDrawVert* vertexBufferMap = (ImDrawVert*)vertexBuffer.MapMemory(vertexBuffer.Size);
		// ushort* indexBufferMap = (ushort*)indexBuffer.MapMemory(indexBuffer.Size);
		//
		// for (int i = 0; i < imDrawData.CmdLists.Size; i++) {
		// 	ImDrawListPtr drawList = imDrawData.CmdLists[i];
		//
		// 	Buffer.MemoryCopy(drawList.VtxBuffer.Data, vertexBufferMap, vertexBuffer.Size, (ulong)(drawList.VtxBuffer.Size * sizeof(ImDrawVert)));
		// 	Buffer.MemoryCopy(drawList.IdxBuffer.Data, indexBufferMap, indexBuffer.Size, (ulong)(drawList.IdxBuffer.Size * sizeof(ushort)));
		//
		// 	vertexBufferMap += drawList.VtxBuffer.Size;
		// 	indexBufferMap += drawList.IdxBuffer.Size;
		// }
		//
		// vertexBuffer.UnmapMemory();
		// indexBuffer.UnmapMemory();
	}

	internal void DrawFrame(GraphicsCommandBuffer commandBuffer, byte frameIndex, ImDrawDataPtr imDrawData) {
		// commandBuffer.CmdBindGraphicsPipeline(graphicsPipeline);
		//
		// commandBuffer.CmdSetViewport(0, 0, (uint)imDrawData.DisplaySize.X, (uint)imDrawData.DisplaySize.Y, 0, 1);
		//
		// commandBuffer.CmdPushConstants(graphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit, new ImGuiPushConstants(new(-1), new(2f / imDrawData.DisplaySize.X, 2f / imDrawData.DisplaySize.Y)));
		// commandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, samplerDescriptorSets.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageFragmentBit);
		//
		// commandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
		// commandBuffer.CmdBindIndexBuffer(indexBuffer, VkIndexType.IndexTypeUint16, indexBuffer.Size); // TODO see if i can do uint32 now
		//
		// Vector2 clipOff = imDrawData.DisplayPos;
		// VkDescriptorSet lastTexture = VkDescriptorSet.Zero;
		//
		// int vertexOffset = 0;
		// uint indexOffset = 0;
		//
		// for (int i = 0; i < imDrawData.CmdLists.Size; i++) {
		// 	ImDrawListPtr cmdList = imDrawData.CmdLists[i];
		//
		// 	for (int j = 0; j < cmdList.CmdBuffer.Size; j++) {
		// 		ImDrawCmd drawCmd = cmdList.CmdBuffer[j];
		//
		// 		Vec2<float> clipMin = new(drawCmd.ClipRect.X - clipOff.X, drawCmd.ClipRect.Y - clipOff.Y);
		// 		Vec2<float> clipMax = new(drawCmd.ClipRect.Z - clipOff.X, drawCmd.ClipRect.W - clipOff.Y);
		//
		// 		if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y) { continue; }
		//
		// 		commandBuffer.CmdSetScissor((int)clipMin.X, (int)clipMin.Y, (uint)(clipMax.X - clipMin.X), (uint)(clipMax.Y - clipMin.Y));
		//
		// 		VkDescriptorSet texture = imageDescriptors[drawCmd.GetTexID()].GetCurrent(frameIndex);
		// 		if (texture != lastTexture) { commandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, texture, VkShaderStageFlagBits.ShaderStageFragmentBit, 1); }
		// 		lastTexture = texture;
		//
		// 		commandBuffer.CmdDrawIndexed(drawCmd.ElemCount, 1, indexOffset, vertexOffset, 0);
		//
		// 		indexOffset += drawCmd.ElemCount;
		// 	}
		//
		// 	vertexOffset += cmdList.VtxBuffer.Size;
		// }
	}

	internal void Cleanup() {
		// TODO
	}

	// [MustUseReturnValue]
	// private IntPtr AddTexture(Texture image, byte maxFramesInFlight) { // TODO add remove
	// 	DescriptorPool descriptorPool = resourceManager.CreateDescriptorPool($"{ImGuiAssetName} Image Descriptor Pool", [ VkDescriptorType.DescriptorTypeSampledImage, ], 1, maxFramesInFlight); // TODO use existing pool
	// 	DescriptorSets descriptorSets = descriptorPool.AllocateDescriptorSets(imageDescriptorSetsLayout);
	// 	descriptorSets.UpdateDescriptorSet(0, image.View);
	//
	// 	nint id = imageDescriptors.Count;
	// 	imageDescriptors[id] = descriptorSets;
	// 	return id;
	// }

	private static void CreatePipeline(VulkanResourceManager resourceManager, VkFormat swapFormatImageFormat, byte maxFramesInFlight, ImGuiFragmentShaderConstants shaderConstants, out GraphicsPipeline graphicsPipeline,
		out DescriptorSets samplerDescriptorSet, out DescriptorSetLayout imageDescriptorSetLayout) {
		VkSpecializationMapEntry specializationMapEntry = new() { constantID = 0, offset = 0, size = sizeof(uint), };
		VkSpecializationInfo specializationInfo = new() { dataSize = (nuint)sizeof(ImGuiFragmentShaderConstants), mapEntryCount = 1, pMapEntries = &specializationMapEntry, pData = &shaderConstants, };

		Assembly assembly = typeof(ImGuiRenderer).Assembly;
		Shader vertexShader = resourceManager.CreateShader($"{ImGuiAssetName} Vertex Shader", assembly, ImGuiAssetName, ShaderLanguage.Glsl, ShaderType.Vertex, specializationInfo);
		Shader fragmentShader = resourceManager.CreateShader($"{ImGuiAssetName} Fragment Shader", assembly, ImGuiAssetName, ShaderLanguage.Glsl, ShaderType.Fragment);

		DescriptorSetLayout samplerDescriptorSetLayout = resourceManager.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);
		imageDescriptorSetLayout = resourceManager.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeSampledImage, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);

		DescriptorPool descriptorPool = resourceManager.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeSampler, ], 1, maxFramesInFlight);

		samplerDescriptorSet = descriptorPool.AllocateDescriptorSets(samplerDescriptorSetLayout);

		VkVertexInputAttributeDescription[] vertexAttributeDescriptions = [
				new() { binding = 0, location = 0, format = VkFormat.FormatR32g32Sfloat, offset = 0, }, //
				new() { binding = 0, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 2, }, //
				new() { binding = 0, location = 2, format = VkFormat.FormatR8g8b8a8Unorm, offset = sizeof(float) * 4, },
		];

		VkVertexInputBindingDescription[] vertexBindingDescriptions = [ new() { binding = 0, stride = (uint)sizeof(ImDrawVert), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

		graphicsPipeline = resourceManager.CreateGraphicsPipeline(
			new($"{ImGuiAssetName} Graphics Pipeline", swapFormatImageFormat, [ vertexShader, fragmentShader, ], vertexAttributeDescriptions, vertexBindingDescriptions) {
					DescriptorSetLayouts = [ samplerDescriptorSetLayout.VkDescriptorSetLayout, imageDescriptorSetLayout.VkDescriptorSetLayout, ],
					PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, offset = 0, size = (uint)sizeof(ImGuiPushConstants), }, ],
					FrontFace = VkFrontFace.FrontFaceClockwise, // ??
					CullMode = VkCullModeFlagBits.CullModeNone, // TODO getting weird artifacts without this set. figure out why and what i should be using
					SrcAlphaBlendFactor = VkBlendFactor.BlendFactorOneMinusSrcAlpha,
			});

		resourceManager.EnqueueDestroy(vertexShader);
		resourceManager.EnqueueDestroy(fragmentShader);
	}

	private static void CreateBuffers(VulkanResourceManager resourceManager, out VulkanBuffer vertexBuffer, out VulkanBuffer indexBuffer) {
		vertexBuffer = resourceManager.CreateBuffer($"{ImGuiAssetName} Vertex Buffer", sizeof(uint), VkBufferUsageFlagBits2.BufferUsage2VertexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

		indexBuffer = resourceManager.CreateBuffer($"{ImGuiAssetName} Index Buffer", sizeof(uint), VkBufferUsageFlagBits2.BufferUsage2IndexBufferBit, 0,
			VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);
	}

	// private static void CreateFont(ImGuiBackend imGuiBackend, VulkanResourceManager resourceManager, TransferCommandPool transferCommandPool, out VulkanTexture fontImage, out TextureSampler textureSampler) {
	// 	imGuiBackend.SetContext(); // may be unnecessary?
	// 	ImGuiIOPtr io = ImGuiNet.GetIO();
	//
	// 	io.Fonts.GetTexDataAsRGBA32(out byte* fontData, out int fontImageWidth, out int fontImageHeight, out int texChannels);
	//
	// 	fontImage = resourceManager.CreateTexture($"{ImGuiAssetName} Font Image", (uint)fontImageWidth, (uint)fontImageHeight, VkFormat.FormatR8g8b8a8Unorm);
	// 	transferCommandPool.CopyToImage(fontImage, physicalGpu.QueueFamilyIndices, logicalGpu.TransferQueue, (uint)fontImageWidth, (uint)fontImageHeight, (byte)texChannels, fontData);
	//
	// 	io.Fonts.ClearTexData(); // do i need to call this?
	//
	// 	textureSampler = resourceManager.CreateTextureSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, physicalGpu.PhysicalDeviceProperties2.properties.limits) {
	// 			AddressMode = new(VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge),
	// 			BorderColor = VkBorderColor.BorderColorFloatOpaqueWhite,
	// 	});
	// }

	[StructLayout(LayoutKind.Explicit, Size = 4)] // i think this needs to be aligned to 4 bytes
	public readonly record struct ImGuiFragmentShaderConstants {
		[field: FieldOffset(0)]
		public bool UseFastLinearColorConversion { get; init; }

		public ImGuiFragmentShaderConstants(bool useFastLinearColorConversion) => UseFastLinearColorConversion = useFastLinearColorConversion;
	}

	private readonly record struct ImGuiPushConstants {
		public Vec2<float> Translate { get; init; }
		public Vec2<float> Scale { get; init; }

		public ImGuiPushConstants(Vec2<float> translate, Vec2<float> scale) {
			Translate = translate;
			Scale = scale;
		}
	}
}