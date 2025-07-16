using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Client.GL.Models;
using USharpLibs.Engine.Client.GL.Shaders;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public static class GLH { // TODO docs
		public static int CurrentShaderHandle { get; private set; }
		public static int CurrentModelVAO { get; private set; }
		public static int CurrentTexture { get; private set; }

		public static bool IsWireframe { get; private set; }
		public static bool IsDepthTesting { get; private set; }
		public static bool IsCulling { get; private set; }

		private static Action? modelDraw;
		private static ShaderWriter? shaderAccess;
		internal static Dictionary<string, int>? CurrentShaderUniformLocations { get; private set; }
		internal static string? CurrentShaderName { get; private set; }

		[MustDisposeResource]
		public static T Bind<T>(Shader<T> shader) where T : ShaderWriter, new() {
			if (CurrentShaderHandle == shader.Handle) { return (T?)shaderAccess ?? throw new NullReferenceException("Somehow shader is both bound and unbound."); }
			if (!shader.WasSetup) { throw new("Shader cannot be bound because it was never setup."); }

			CurrentShaderUniformLocations = shader.UniformLocations;
			CurrentShaderName = shader.ShaderName;

			T access = new() { OriginalShaderHandle = CurrentShaderHandle = shader.Handle, };
			shaderAccess = access;

			OpenGL4.UseProgram(CurrentShaderHandle);
			return access;
		}

		public static void Bind(Model model) {
			if (CurrentModelVAO == model.VAO) { return; }
			if (!model.WasSetup) {
				Logger.Warn("Attempted to bind model that was not setup!");
				return;
			}

			OpenGL4.BindVertexArray(CurrentModelVAO = model.VAO);
			modelDraw = model.Draw;
		}

		/// <summary> Binds the provided texture for use. </summary>
		/// <param name="texture"> The texture to bind and use. </param>
		/// <param name="unit"> Which <see cref="TextureUnit"/> to use. Default is <see cref="TextureUnit.Texture0"/>. </param>
		public static void Bind(Texture texture, TextureUnit unit = TextureUnit.Texture0) {
			if (CurrentTexture == texture.Handle) { return; }
			if (!texture.WasSetup) {
				Logger.Warn("Texture was not setup!");
				return;
			}

			CurrentTexture = texture.Handle;
			OpenGL4.ActiveTexture(unit);
			OpenGL4.BindTexture(TextureTarget.Texture2D, texture.Handle);
		}

		public static void UnbindShader() {
			if (CurrentShaderHandle == 0) { return; }
			CurrentShaderHandle = 0;
			CurrentShaderUniformLocations = null;
			CurrentShaderName = null;
			OpenGL4.UseProgram(0);
		}

		public static void UnbindModel() {
			if (CurrentModelVAO == 0) { return; }
			CurrentModelVAO = 0;
			modelDraw = null;
			OpenGL4.BindVertexArray(0);
		}

		/// <summary> Unbinds the current texture. </summary>
		/// <param name="force"> Whether or not to ignore any checks and force a texture unbind. </param>
		public static void UnbindTexture(bool force = false) {
			if (CurrentTexture == 0 && !force) { return; }

			CurrentTexture = 0;
			OpenGL4.BindTexture(TextureTarget.Texture2D, 0);
		}

		public static void DrawModel() {
			if (CurrentShaderHandle == 0) {
				Logger.Warn("Attempted to draw model with no shader bound!");
				return;
			} else if (CurrentModelVAO == 0) {
				Logger.Warn("Attempted to draw model with no model bound!");
				return;
			}

			if (modelDraw != null) { modelDraw.Invoke(); } else { throw new NullReferenceException("Somehow you tried to draw a model that is both bound and unbound."); }
		}

		/// <summary> Enables Wireframe mode. </summary>
		public static void EnableWireframe() {
			if (!IsWireframe) {
				OpenGL4.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
				IsWireframe = true;
			}
		}

		/// <summary> Disables Wireframe mode. </summary>
		public static void DisableWireframe() {
			if (IsWireframe) {
				OpenGL4.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
				IsWireframe = false;
			}
		}

		/// <summary> Enables Depth Testing. </summary>
		public static void EnableDepthTest() {
			if (!IsDepthTesting) {
				OpenGL4.Enable(EnableCap.DepthTest);
				IsDepthTesting = true;
			}
		}

		/// <summary> Disables Depth Testing. </summary>
		public static void DisableDepthTest() {
			if (IsDepthTesting) {
				OpenGL4.Disable(EnableCap.DepthTest);
				IsDepthTesting = false;
			}
		}

		/// <summary> Enables Culling. </summary>
		public static void EnableCulling() {
			if (!IsCulling) {
				OpenGL4.Enable(EnableCap.CullFace);
				IsCulling = true;
			}
		}

		/// <summary> Disables Culling. </summary>
		public static void DisableCulling() {
			if (IsCulling) {
				OpenGL4.Disable(EnableCap.CullFace);
				IsCulling = false;
			}
		}
	}
}