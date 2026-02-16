using System.Numerics;
using Engine3.Client.Graphics.ImGui;
using Engine3.Client.Graphics.OpenGL.Objects;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL {
	public unsafe class OpenGLImGuiBackend : ImGuiBackend<OpenGLResourceProvider> {
		private OpenGLShader vertexShader = null!;
		private OpenGLShader fragmentShader = null!;
		private ProgramPipeline programPipeline = null!;

		private OpenGLBuffer vertexBuffer = null!; // Frames-in-flight? i think i read somewhere i should? look into
		private OpenGLBuffer indexBuffer = null!; // Frames-in-flight?

		private OpenGLImage fontImage = null!;

		public OpenGLImGuiBackend(Window window) : base(window, GraphicsBackend.OpenGL, new()) { }

		public void Setup() {
			ImGuiNet.SetCurrentContext(Context);
			ImGuiIOPtr io = ImGuiNet.GetIO();

			vertexShader = GraphicsResourceProvider.CreateShader($"{ImGuiName} Vertex Shader", ImGuiName, ShaderType.Vertex, Engine3.Assembly);
			fragmentShader = GraphicsResourceProvider.CreateShader($"{ImGuiName} Fragment Shader", ImGuiName, ShaderType.Fragment, Engine3.Assembly);
			programPipeline = GraphicsResourceProvider.CreateProgramPipeline($"{ImGuiName} Program Pipeline", vertexShader, fragmentShader);

			// GraphicsResourceProvider.EnqueueDestroy(vertexShader);
			// GraphicsResourceProvider.EnqueueDestroy(fragmentShader);

			vertexBuffer = GraphicsResourceProvider.CreateBuffer($"{ImGuiName} Vertex Buffer", BufferStorageMask.DynamicStorageBit, 1);
			indexBuffer = GraphicsResourceProvider.CreateBuffer($"{ImGuiName} Index Buffer", BufferStorageMask.DynamicStorageBit, 1);

			io.Fonts.GetTexDataAsRGBA32(out byte* fontData, out int fontImageWidth, out int fontImageHeight, out _);

			fontImage = GraphicsResourceProvider.CreateImage($"{ImGuiName} Font Image");
			fontImage.Copy(fontData, (uint)fontImageWidth, (uint)fontImageHeight);

			io.Fonts.ClearTexData(); // do i need to call this?
		}

		public override void UpdateBuffers(ImDrawDataPtr drawData) {
			if (drawData.TotalVtxCount > (uint)(vertexBuffer.BufferSize / (uint)sizeof(ImDrawVert))) {
				GraphicsResourceProvider.EnqueueDestroy(vertexBuffer);
				vertexBuffer = GraphicsResourceProvider.CreateBuffer(vertexBuffer.DebugName, BufferStorageMask.DynamicStorageBit, (ulong)(drawData.TotalVtxCount * sizeof(ImDrawVert)));
			}

			if (drawData.TotalIdxCount > (uint)(indexBuffer.BufferSize / sizeof(uint))) {
				GraphicsResourceProvider.EnqueueDestroy(indexBuffer);
				indexBuffer = GraphicsResourceProvider.CreateBuffer(indexBuffer.DebugName, BufferStorageMask.DynamicStorageBit, (ulong)(drawData.TotalIdxCount * sizeof(uint)));
			}

			// do i copy just when the data is different? i feel like i should but i don't know how to check that easily
		}

		public void DrawFrame(ImDrawDataPtr drawData) {
			bool cullFace;
			bool depthTest;
			bool stencilTest;
			bool scissorTest;
			bool blend;

			BlendingFactor blendSrcFactorRgb;
			BlendingFactor blendDstFactorRgb;
			BlendingFactor blendSrcFactorAlpha;
			BlendingFactor blendDstFactorAlpha;
			BlendEquationMode blendEquationRgb;
			BlendEquationMode blendEquationAlpha;

			int* lastScissorBox = stackalloc int[4];

			StoreState();
			SetupState();

			Vector2 translate = new(-1);
			Vector2 scale = new(2f / drawData.DisplaySize.X, 2f / drawData.DisplaySize.Y);

			translate = translate with { Y = -translate.Y, };
			scale = scale with { Y = -scale.Y, };

			vertexShader.SetUniform("translate", translate);
			vertexShader.SetUniform("scale", scale);

			Vector2 clipOff = drawData.DisplayPos;

			for (int i = 0; i < drawData.CmdListsCount; i++) {
				ImDrawListPtr cmdList = drawData.CmdLists[i];

				// Copy buffer. TODO move above & offset buffer instead
				uint[] indices32 = new uint[cmdList.IdxBuffer.Size];

				// TODO can i do this without iterating?
				for (int j = 0; j < cmdList.IdxBuffer.Size; j++) { indices32[j] = ((ushort*)cmdList.IdxBuffer.Data)[j]; } // atm my shader uses vertex pulling which needs uint32

				vertexBuffer.Copy((void*)cmdList.VtxBuffer.Data, (ulong)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert)));
				indexBuffer.Copy(indices32);

				for (int j = 0; j < cmdList.CmdBuffer.Size; j++) {
					ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[j];

					Vector2 clipMin = new(drawCmd.ClipRect.X - clipOff.X, drawCmd.ClipRect.Y - clipOff.Y);
					Vector2 clipMax = new(drawCmd.ClipRect.Z - clipOff.X, drawCmd.ClipRect.W - clipOff.Y);

					if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y) { continue; }

					GL.Scissor((int)clipMin.X, (int)(drawData.DisplaySize.Y - clipMax.Y), (int)(clipMax.X - clipMin.X), (int)(clipMax.Y - clipMin.Y));
					GL.DrawArrays(PrimitiveType.Triangles, (int)drawCmd.IdxOffset, (int)drawCmd.ElemCount);
				}
			}

			RestoreState();

			return;

			void StoreState() {
				cullFace = GL.IsEnabled(EnableCap.CullFace);
				depthTest = GL.IsEnabled(EnableCap.DepthTest);
				stencilTest = GL.IsEnabled(EnableCap.StencilTest);
				scissorTest = GL.IsEnabled(EnableCap.ScissorTest);
				blend = GL.IsEnabled(EnableCap.Blend);

				blendSrcFactorRgb = (BlendingFactor)GL.GetInteger(GetPName.BlendSrcRgb);
				blendDstFactorRgb = (BlendingFactor)GL.GetInteger(GetPName.BlendDstRgb);
				blendSrcFactorAlpha = (BlendingFactor)GL.GetInteger(GetPName.BlendSrcAlpha);
				blendDstFactorAlpha = (BlendingFactor)GL.GetInteger(GetPName.BlendDstAlpha);
				blendEquationRgb = (BlendEquationMode)GL.GetInteger(GetPName.BlendEquationRgb);
				blendEquationAlpha = (BlendEquationMode)GL.GetInteger(GetPName.BlendEquationAlpha);

				GL.GetIntegerv(GetPName.ScissorBox, lastScissorBox);
			}

			void SetupState() {
				GL.Disable(EnableCap.CullFace);
				GL.Disable(EnableCap.DepthTest);
				GL.Disable(EnableCap.StencilTest);
				GL.Enable(EnableCap.ScissorTest);
				GL.Enable(EnableCap.Blend);

				GL.BlendEquation(BlendEquationMode.FuncAdd);
				GL.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

				GL.BindProgramPipeline(programPipeline.ProgramPipelineHandle.Handle);

				GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, (int)vertexBuffer.BufferHandle);
				GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, (int)indexBuffer.BufferHandle);

				GL.BindTexture(TextureTarget.Texture2d, (int)fontImage.TextureHandle);
			}

			void RestoreState() {
				if (cullFace) { GL.Enable(EnableCap.CullFace); } else { GL.Disable(EnableCap.CullFace); }
				if (depthTest) { GL.Enable(EnableCap.DepthTest); } else { GL.Disable(EnableCap.DepthTest); }
				if (stencilTest) { GL.Enable(EnableCap.StencilTest); } else { GL.Disable(EnableCap.StencilTest); }
				if (scissorTest) { GL.Enable(EnableCap.ScissorTest); } else { GL.Disable(EnableCap.ScissorTest); }
				if (blend) { GL.Enable(EnableCap.Blend); } else { GL.Disable(EnableCap.Blend); }

				GL.BlendEquationSeparate(blendEquationRgb, blendEquationAlpha);
				GL.BlendFuncSeparate(blendSrcFactorRgb, blendDstFactorRgb, blendSrcFactorAlpha, blendDstFactorAlpha);

				GL.Scissor(lastScissorBox[0], lastScissorBox[1], lastScissorBox[2], lastScissorBox[3]);
			}
		}
	}
}