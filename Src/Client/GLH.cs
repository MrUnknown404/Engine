using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client {
	[PublicAPI]
	public static class GLH {
		// public static GLErrorHandlingTypes GLErrorHandlingTypes { get; set; } = GLErrorHandlingTypes.Throw; // TODO put into Debug class. also add more debug options

		public static bool IsWireframe { get; private set; }
		public static bool IsDepthTesting { get; private set; }
		public static bool IsCulling { get; private set; }
		public static bool IsBlend { get; private set; }
		public static BlendingFactor SFactor { get; private set; } = BlendingFactor.One;
		public static BlendingFactor DFactor { get; private set; } = BlendingFactor.Zero;

		public static ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;

		// public static uint CurrentShaderHandle { get; private set; }
		// private static ModelAccess ModelAccess { get; } = new();

		// [MustUseReturnValue]
		// public static T Bind<T>(Shader<T> shader) where T : ShaderAccess, new() {
		// 	if (ShaderErrorHandler.Assert(shader.Handle == 0, () => new(shader, ShaderErrorHandler.Reason.NoHandle))) { return shader.Access; }
		//
		// 	if (CurrentShaderHandle != shader.Handle) {
		// 		CurrentShaderHandle = shader.Handle;
		// 		GL.UseProgram(CurrentShaderHandle);
		// 	}
		//
		// 	return shader.Access;
		// }

		// [MustUseReturnValue]
		// public static ModelAccess Bind(Model model) {
		// 	if (ModelErrorHandler.Assert(model.VAO == 0, static () => new(ModelErrorHandler.Reason.NoVAO))) { return ModelAccess; }
		// 	if (ModelErrorHandler.Assert(model.WasFreed, static () => new(ModelErrorHandler.Reason.WasFreed))) { return ModelAccess; }
		//
		// 	if (ModelAccess.Model == null || ModelAccess.Model.VAO != model.VAO) {
		// 		ModelAccess.Model = model;
		// 		GL.BindVertexArray(model.VAO);
		// 	}
		//
		// 	return ModelAccess;
		// }

		// public static void UnbindShader() {
		// 	if (CurrentShaderHandle == 0) { return; }
		// 	CurrentShaderHandle = 0;
		// 	GL.UseProgram(0);
		// }

		// public static void UnbindModel() {
		// 	if (ModelAccess.Model == null) { return; }
		// 	ModelAccess.Model = null;
		// 	GL.BindVertexArray(0);
		// }

		/// <summary> Enables Wireframe mode. </summary>
		public static void EnableWireframe() {
			if (!IsWireframe) {
				GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
				IsWireframe = true;
			}
		}

		/// <summary> Disables Wireframe mode. </summary>
		public static void DisableWireframe() {
			if (IsWireframe) {
				GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
				IsWireframe = false;
			}
		}

		/// <summary> Enables Depth Testing. </summary>
		public static void EnableDepthTest() {
			if (!IsDepthTesting) {
				GL.Enable(EnableCap.DepthTest);
				IsDepthTesting = true;
			}
		}

		/// <summary> Disables Depth Testing. </summary>
		public static void DisableDepthTest() {
			if (IsDepthTesting) {
				GL.Disable(EnableCap.DepthTest);
				IsDepthTesting = false;
			}
		}

		/// <summary> Enables Culling. </summary>
		public static void EnableCulling() {
			if (!IsCulling) {
				GL.Enable(EnableCap.CullFace);
				IsCulling = true;
			}
		}

		/// <summary> Disables Culling. </summary>
		public static void DisableCulling() {
			if (IsCulling) {
				GL.Disable(EnableCap.CullFace);
				IsCulling = false;
			}
		}

		public static void EnableBlend() {
			if (!IsBlend) {
				GL.Enable(EnableCap.Blend);
				IsBlend = true;
			}
		}

		public static void EnableBlend(BlendingFactor sFactor, BlendingFactor dFactor) {
			if (!IsBlend) {
				GL.Enable(EnableCap.Blend);
				IsBlend = true;
				SetBlendFunc(sFactor, dFactor);
			}
		}

		public static void DisableBlend() {
			if (IsBlend) {
				GL.Disable(EnableCap.Blend);
				IsBlend = false;
			}
		}

		public static void SetBlendFunc(BlendingFactor sFactor, BlendingFactor dFactor) {
			if (sFactor != SFactor || dFactor != DFactor) {
				GL.BlendFunc(sFactor, dFactor);
				SFactor = sFactor;
				DFactor = dFactor;
			}
		}
	}
}