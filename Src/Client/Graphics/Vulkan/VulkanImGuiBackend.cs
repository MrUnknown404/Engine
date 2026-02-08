using System.Numerics;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics.Vulkan.Objects;
using ImGuiNET;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client.Graphics.Vulkan {
	// TODO allow uint indices. edit: i think imgui was compiled for 16 bit indices. don't know if i can change that

	public unsafe class VulkanImGuiBackend : ImGuiBackend<LogicalGpu> {
		public ImGuiFragmentShaderConstants ImGuiShaderConstants { get; init; } = new(true);

		private GraphicsPipeline graphicsPipeline = null!;
		private DescriptorSets descriptorSet = null!;
		private TextureSampler textureSampler = null!;
		private VulkanImage fontImage = null!;
		private VulkanBuffer vertexBuffer = null!; // Frames-in-flight? i think i read somewhere i should? look into
		private VulkanBuffer indexBuffer = null!; // Frames-in-flight?

		private readonly SurfaceCapablePhysicalGpu physicalGpu;
		private readonly byte maxFramesInFlight;

		public VulkanImGuiBackend(VulkanWindow window, byte maxFramesInFlight) : base(window, GraphicsBackend.Vulkan, window.LogicalGpu) {
			physicalGpu = window.SelectedGpu;
			this.maxFramesInFlight = maxFramesInFlight;
		}

		public void Setup(VkCommandPool transferCommandPool, VkFormat swapFormatImageFormat) {
			ImGuiFragmentShaderConstants shaderConstants = ImGuiShaderConstants;
			VkSpecializationMapEntry specializationMapEntry = new() { constantID = 0, size = sizeof(uint), offset = 0, };
			VkSpecializationInfo specializationInfo = new() { dataSize = (nuint)sizeof(ImGuiFragmentShaderConstants), mapEntryCount = 1, pMapEntries = &specializationMapEntry, pData = &shaderConstants, };

			VulkanShader vertexShader = GraphicsResourceProvider.CreateShader($"{ImGuiName} Vertex Shader", ImGuiName, ShaderLanguage.Glsl, ShaderType.Vertex, Engine3.Assembly, specializationInfo);
			VulkanShader fragmentShader = GraphicsResourceProvider.CreateShader($"{ImGuiName} Fragment Shader", ImGuiName, ShaderLanguage.Glsl, ShaderType.Fragment, Engine3.Assembly);

			DescriptorSetLayout descriptorSetLayout = GraphicsResourceProvider.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);

			DescriptorPool descriptorPool = GraphicsResourceProvider.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1, maxFramesInFlight);
			descriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);

			VkVertexInputAttributeDescription[] vertexAttributeDescriptions = [
					new() { binding = 0, location = 0, format = VkFormat.FormatR32g32Sfloat, offset = 0, }, //
					new() { binding = 0, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 2, }, //
					new() { binding = 0, location = 2, format = VkFormat.FormatR8g8b8a8Unorm, offset = sizeof(float) * 4, },
			];

			VkVertexInputBindingDescription[] vertexBindingDescriptions = [ new() { binding = 0, stride = (uint)sizeof(ImDrawVert), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

			graphicsPipeline = GraphicsResourceProvider.CreateGraphicsPipeline(
				new($"{ImGuiName} Graphics Pipeline", swapFormatImageFormat, [ vertexShader, fragmentShader, ], vertexAttributeDescriptions, vertexBindingDescriptions) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, offset = 0, size = (uint)sizeof(ImGuiPushConstants), }, ],
						EnableDepthTest = false,
						CullMode = VkCullModeFlagBits.CullModeNone, // TODO getting weird artifacts without this set. figure out why and what i should be using
						SrcAlphaBlendFactor = VkBlendFactor.BlendFactorOneMinusSrcAlpha,
				});

			GraphicsResourceProvider.EnqueueDestroy(vertexShader);
			GraphicsResourceProvider.EnqueueDestroy(fragmentShader);

			vertexBuffer = GraphicsResourceProvider.CreateBuffer($"{ImGuiName} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, 1);

			indexBuffer = GraphicsResourceProvider.CreateBuffer($"{ImGuiName} Index Buffer", VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, 1);

			ImGui.SetCurrentContext(Context);
			ImGuiIOPtr io = ImGui.GetIO();

			io.Fonts.GetTexDataAsRGBA32(out byte* fontData, out int fontImageWidth, out int fontImageHeight, out int texChannels);

			fontImage = GraphicsResourceProvider.CreateImage($"{ImGuiName} Font Image", (uint)fontImageWidth, (uint)fontImageHeight, VkFormat.FormatR8g8b8a8Unorm);
			fontImage.CopyUsingStaging(transferCommandPool, GraphicsResourceProvider.TransferQueue, (uint)fontImageWidth, (uint)fontImageHeight, (byte)texChannels, fontData);

			io.Fonts.ClearTexData(); // do i need to call this?

			textureSampler = GraphicsResourceProvider.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, physicalGpu.PhysicalDeviceProperties2.properties.limits) {
					AddressMode = new(VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge),
					BorderColor = VkBorderColor.BorderColorFloatOpaqueWhite,
			});

			descriptorSet.UpdateDescriptorSet(0, fontImage.ImageView, textureSampler.Sampler);
		}

		public override void UpdateBuffers(ImDrawDataPtr drawData) {
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

		public void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, byte frameIndex, ImDrawDataPtr drawData) {
			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, (uint)drawData.DisplaySize.X, (uint)drawData.DisplaySize.Y, 0, 1);

			graphicsCommandBuffer.CmdPushConstants(graphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit, 0, new ImGuiPushConstants(new(-1), new(2f / drawData.DisplaySize.X, 2f / drawData.DisplaySize.Y)));

			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, descriptorSet.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageFragmentBit);

			graphicsCommandBuffer.CmdBindVertexBuffer(vertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(indexBuffer, indexBuffer.BufferSize, VkIndexType.IndexTypeUint16);

			Vector2 clipOff = drawData.DisplayPos;

			int vertexOffset = 0;
			uint indexOffset = 0;

			for (int i = 0; i < drawData.CmdListsCount; i++) {
				ImDrawListPtr cmdList = drawData.CmdLists[i];

				for (int j = 0; j < cmdList.CmdBuffer.Size; j++) {
					ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[j];

					Vector2 clipMin = new(drawCmd.ClipRect.X - clipOff.X, drawCmd.ClipRect.Y - clipOff.Y);
					Vector2 clipMax = new(drawCmd.ClipRect.Z - clipOff.X, drawCmd.ClipRect.W - clipOff.Y);

					if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y) { continue; }

					graphicsCommandBuffer.CmdSetScissor(new((int)clipMin.X, (int)clipMin.Y), new((uint)(clipMax.X - clipMin.X), (uint)(clipMax.Y - clipMin.Y)));
					graphicsCommandBuffer.CmdDrawIndexed(drawCmd.ElemCount, 1, indexOffset, vertexOffset, 0);

					indexOffset += drawCmd.ElemCount;
				}

				vertexOffset += cmdList.VtxBuffer.Size;
			}
		}

		private readonly record struct ImGuiPushConstants {
			public Vector2 Translate { get; init; }
			public Vector2 Scale { get; init; }

			public ImGuiPushConstants(Vector2 translate, Vector2 scale) {
				Translate = translate;
				Scale = scale;
			}
		}

		[StructLayout(LayoutKind.Explicit, Size = 4)] // i think this needs to be aligned to 4 bytes
		public readonly record struct ImGuiFragmentShaderConstants {
			[field: FieldOffset(0)] public bool UseFastLinearColorConversion { get; init; }

			public ImGuiFragmentShaderConstants(bool useFastLinearColorConversion) => UseFastLinearColorConversion = useFastLinearColorConversion;
		}
	}
}