using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Engine2.Client.Models;
using USharpLibs.Engine2.Client.Shaders;

namespace USharpLibs.Engine2.Client {
	[PublicAPI]
	public static class GLH {
		public static bool IsWireframe { get; private set; }
		public static bool IsDepthTesting { get; private set; }
		public static bool IsCulling { get; private set; }

		public static uint CurrentShaderHandle { get; private set; }
		private static ModelAccess ModelAccess { get; } = new();

		[MustUseReturnValue]
		public static T? Bind<T>(Shader<T> shader) where T : ShaderAccess, new() {
			if (shader.Handle == 0) {
				Logger.Warn($"Tried to bind empty shader! Shader: {shader.DebugName}");
				return null;
			}

			if (CurrentShaderHandle != shader.Handle) {
				CurrentShaderHandle = shader.Handle;
				GL.UseProgram(CurrentShaderHandle);
			}

			return shader.Access;
		}

		[MustUseReturnValue]
		public static ModelAccess Bind(Model model) {
			if (model.VAO == 0) { throw new ModelAccessException(ModelAccessException.Reason.NoVAO); }
			if (model.WasFreed) { throw new ModelAccessException(ModelAccessException.Reason.WasFreed); }

			if (ModelAccess.Model == null || ModelAccess.Model.VAO != model.VAO) {
				ModelAccess.Model = model;
				GL.BindVertexArray(model.VAO);
			}

			return ModelAccess;
		}

		public static void UnbindShader() {
			if (CurrentShaderHandle == 0) { return; }
			CurrentShaderHandle = 0;
			GL.UseProgram(0);
		}

		public static void UnbindModel() {
			if (ModelAccess.Model == null) { return; }
			ModelAccess.Model = null;
			GL.BindVertexArray(0);
		}

		/// <summary> Enables Wireframe mode. </summary>
		public static void EnableWireframe() {
			if (!IsWireframe) {
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
				IsWireframe = true;
			}
		}

		/// <summary> Disables Wireframe mode. </summary>
		public static void DisableWireframe() {
			if (IsWireframe) {
				GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
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
	}
}