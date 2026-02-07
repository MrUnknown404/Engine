using System.Numerics;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using ImGuiNET;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Client {
	// TODO allow uint indices. edit: i think imgui was compiled for 16 bit indices. don't know if i can change that

	public unsafe class VulkanImGuiBackend : ImGuiBackend {
		private const string ImGuiName = "ImGui";

		public ImGuiFragmentShaderConstants ImGuiShaderConstants { get; init; } = new(true);
		public Action? AddImGuiWindows { get; init; } = static () => ImGui.ShowDemoWindow();

		private GraphicsPipeline imGuiGraphicsPipeline = null!;
		private DescriptorSets imGuiDescriptorSet = null!;
		private TextureSampler imGuiTextureSampler = null!;
		private VulkanImage imGuiFontImage = null!;
		private VulkanBuffer imGuiVertexBuffer = null!; // Frames-in-flight? i think i read somewhere i should? look into
		private VulkanBuffer imGuiIndexBuffer = null!; // Frames-in-flight?

		private readonly VulkanWindow window;
		private readonly byte maxFramesInFlight;

		private SurfaceCapablePhysicalGpu PhysicalGpu => window.SelectedGpu;
		private LogicalGpu LogicalGpu => window.LogicalGpu;

		public VulkanImGuiBackend(VulkanWindow window, byte maxFramesInFlight) {
			this.window = window;
			this.maxFramesInFlight = maxFramesInFlight;
		}

		public void Setup(VkCommandPool transferCommandPool, VkFormat swapFormatImageFormat) {
			ImGuiFragmentShaderConstants shaderConstants = ImGuiShaderConstants;
			VkSpecializationMapEntry specializationMapEntry = new() { constantID = 0, size = sizeof(uint), offset = 0, };
			VkSpecializationInfo specializationInfo = new() { dataSize = (nuint)sizeof(ImGuiFragmentShaderConstants), mapEntryCount = 1, pMapEntries = &specializationMapEntry, pData = &shaderConstants, };

			VulkanShader vertexShader = LogicalGpu.CreateShader($"{ImGuiName} Vertex Shader", ImGuiName, ShaderLanguage.Glsl, ShaderType.Vertex, Engine3.Assembly, specializationInfo);
			VulkanShader fragmentShader = LogicalGpu.CreateShader($"{ImGuiName} Fragment Shader", ImGuiName, ShaderLanguage.Glsl, ShaderType.Fragment, Engine3.Assembly);

			DescriptorSetLayout descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeCombinedImageSampler, VkShaderStageFlagBits.ShaderStageFragmentBit, 0), ]);

			DescriptorPool descriptorPool = LogicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, VkDescriptorType.DescriptorTypeCombinedImageSampler, ], 1, maxFramesInFlight);
			imGuiDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);

			VkVertexInputAttributeDescription[] vertexAttributeDescriptions = [
					new() { binding = 0, location = 0, format = VkFormat.FormatR32g32Sfloat, offset = 0, }, //
					new() { binding = 0, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 2, }, //
					new() { binding = 0, location = 2, format = VkFormat.FormatR8g8b8a8Unorm, offset = sizeof(float) * 4, },
			];

			VkVertexInputBindingDescription[] vertexBindingDescriptions = [ new() { binding = 0, stride = (uint)sizeof(ImDrawVert), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

			imGuiGraphicsPipeline = LogicalGpu.CreateGraphicsPipeline(new($"{ImGuiName} Graphics Pipeline", swapFormatImageFormat, [ vertexShader, fragmentShader, ], vertexAttributeDescriptions, vertexBindingDescriptions) {
					DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
					PushConstantRanges = [ new() { stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, offset = 0, size = (uint)sizeof(ImGuiPushConstants), }, ],
					EnableDepthTest = false,
					CullMode = VkCullModeFlagBits.CullModeNone, // TODO getting weird artifacts without this set. figure out why and what i should be using
					SrcAlphaBlendFactor = VkBlendFactor.BlendFactorOneMinusSrcAlpha,
			});

			LogicalGpu.EnqueueDestroy(vertexShader);
			LogicalGpu.EnqueueDestroy(fragmentShader);

			imGuiVertexBuffer = LogicalGpu.CreateBuffer($"{ImGuiName} Vertex Buffer", VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, 1);

			imGuiIndexBuffer = LogicalGpu.CreateBuffer($"{ImGuiName} Index Buffer", VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, 1);

			ImGuiIOPtr io = ImGui.GetIO();
			io.Fonts.GetTexDataAsRGBA32(out byte* fontData, out int fontImageWidth, out int fontImageHeight, out int texChannels);

			imGuiFontImage = LogicalGpu.CreateImage($"{ImGuiName} Font Image", (uint)fontImageWidth, (uint)fontImageHeight, VkFormat.FormatR8g8b8a8Unorm);
			imGuiFontImage.CopyUsingStaging(transferCommandPool, LogicalGpu.TransferQueue, (uint)fontImageWidth, (uint)fontImageHeight, (byte)texChannels, fontData);

			io.Fonts.ClearTexData(); // do i need to call this?

			imGuiTextureSampler = LogicalGpu.CreateSampler(new(VkFilter.FilterLinear, VkFilter.FilterLinear, PhysicalGpu.PhysicalDeviceProperties2.properties.limits) {
					AddressMode = new(VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge, VkSamplerAddressMode.SamplerAddressModeClampToEdge),
					BorderColor = VkBorderColor.BorderColorFloatOpaqueWhite,
			});

			imGuiDescriptorSet.UpdateDescriptorSet(0, imGuiFontImage.ImageView, imGuiTextureSampler.Sampler);
		}

		public override bool NewFrame(out ImDrawDataPtr imDrawData) {
			if (AddImGuiWindows == null) {
				imDrawData = new();
				return false;
			}

			ImGuiH.NewFrame(window);

			AddImGuiWindows.Invoke();

			ImGuiH.EndFrame();

			ImGui.Render();

			imDrawData = ImGui.GetDrawData();
			return imDrawData is { Valid: true, CmdListsCount: > 0, };
		}

		public override void UpdateBuffers(ImDrawDataPtr imDrawData) {
			if (imDrawData.TotalVtxCount > (uint)(imGuiVertexBuffer.BufferSize / (uint)sizeof(ImDrawVert))) {
				LogicalGpu.EnqueueDestroy(imGuiVertexBuffer);

				imGuiVertexBuffer = LogicalGpu.CreateBuffer(imGuiVertexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
					VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, (ulong)(imDrawData.TotalVtxCount * sizeof(ImDrawVert)));
			}

			if (imDrawData.TotalIdxCount > (uint)(imGuiIndexBuffer.BufferSize / sizeof(ushort))) {
				LogicalGpu.EnqueueDestroy(imGuiIndexBuffer);

				imGuiIndexBuffer = LogicalGpu.CreateBuffer(imGuiIndexBuffer.DebugName, VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
					VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, (ulong)(imDrawData.TotalIdxCount * sizeof(ushort)));
			}

			// do i copy just when the data is different? i feel like i should but i don't know how to check that easily

			ImDrawVert* vertexBufferMap = (ImDrawVert*)imGuiVertexBuffer.MapMemory(imGuiVertexBuffer.BufferSize);
			ushort* indexBufferMap = (ushort*)imGuiIndexBuffer.MapMemory(imGuiIndexBuffer.BufferSize);

			for (int i = 0; i < imDrawData.CmdListsCount; i++) {
				ImDrawListPtr drawList = imDrawData.CmdLists[i];

				Buffer.MemoryCopy((void*)drawList.VtxBuffer.Data, vertexBufferMap, imGuiVertexBuffer.BufferSize, (ulong)(drawList.VtxBuffer.Size * sizeof(ImDrawVert)));
				Buffer.MemoryCopy((void*)drawList.IdxBuffer.Data, indexBufferMap, imGuiIndexBuffer.BufferSize, (ulong)(drawList.IdxBuffer.Size * sizeof(ushort)));

				vertexBufferMap += drawList.VtxBuffer.Size;
				indexBufferMap += drawList.IdxBuffer.Size;
			}

			imGuiVertexBuffer.UnmapMemory();
			imGuiIndexBuffer.UnmapMemory();
		}

		public void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, byte frameIndex, ImDrawDataPtr drawData) {
			graphicsCommandBuffer.CmdBindGraphicsPipeline(imGuiGraphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, (uint)drawData.DisplaySize.X, (uint)drawData.DisplaySize.Y, 0, 1);

			graphicsCommandBuffer.CmdPushConstants(imGuiGraphicsPipeline.Layout, VkShaderStageFlagBits.ShaderStageVertexBit, 0, new ImGuiPushConstants(new(-1), new(2f / drawData.DisplaySize.X, 2f / drawData.DisplaySize.Y)));

			graphicsCommandBuffer.CmdBindDescriptorSet(imGuiGraphicsPipeline.Layout, imGuiDescriptorSet.GetCurrent(frameIndex), VkShaderStageFlagBits.ShaderStageFragmentBit);

			graphicsCommandBuffer.CmdBindVertexBuffer(imGuiVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(imGuiIndexBuffer, imGuiIndexBuffer.BufferSize, VkIndexType.IndexTypeUint16);

			long vertexOffset = 0;
			long indexOffset = 0;

			for (int i = 0; i < drawData.CmdListsCount; i++) {
				ImDrawListPtr cmdList = drawData.CmdLists[i];

				for (int j = 0; j < cmdList.CmdBuffer.Size; j++) {
					ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[j];

					graphicsCommandBuffer.CmdSetScissor(new((int)Math.Max(drawCmd.ClipRect.X, 0), (int)Math.Max(drawCmd.ClipRect.Y, 0)),
						new((uint)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X), (uint)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)));

					graphicsCommandBuffer.CmdDrawIndexed(drawCmd.ElemCount, 1, (uint)indexOffset, (int)vertexOffset, 0);
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