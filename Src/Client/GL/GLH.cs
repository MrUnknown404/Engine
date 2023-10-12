using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Client.GL.Models;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public static class GLH {
		public static int CurrentShader { get; private set; }
		public static int CurrentVAO { get; private set; }
		public static int CurrentTexture { get; private set; }

		public static bool IsWireframe { get; private set; }
		public static bool IsDepthTesting { get; private set; }
		public static bool IsCulling { get; private set; }

		/// <summary> Binds the provided shader for use. </summary>
		/// <typeparam name="T"> The type of shader that is in use. </typeparam>
		/// <param name="shader"> An unbound shader to bind and use. </param>
		public static void Rebind<T>(UnboundShader<T> shader) where T : Shader {
			T s = shader.Shader;
			if (!s.WasSetup) {
				Logger.Warn("Shader was not setup!");
				return;
			} else if (CurrentShader == s.Handle) { return; }

			CurrentShader = s.Handle;
			OpenGL4.UseProgram(s.Handle);
		}

		/// <summary> Binds the provided shader for use. </summary>
		/// <typeparam name="T"> The type of shader that is in use. </typeparam>
		/// <param name="shader"> An unbound shader to bind and use. </param>
		/// <param name="use"> An Action to run using the bound shader. </param>
		public static void Bind<T>(UnboundShader<T> shader, Action<T> use) where T : Shader {
			T s = shader.Shader;
			if (!s.WasSetup) {
				Logger.Warn("Shader was not setup!");
				return;
			} else if (CurrentShader == s.Handle) {
				use(s);
				return;
			}

			CurrentShader = s.Handle;
			OpenGL4.UseProgram(s.Handle);
			use(s);
		}

		/// <summary> Binds the provided model for use. </summary>
		/// <typeparam name="T"> The type of model that will be returned. </typeparam>
		/// <param name="model"> An unbound model to bind and use. </param>
		/// <returns> A bound model object if everything was successful. If an error occurred null will be returned. </returns>
		[MustUseReturnValue]
		public static T? Bind<T>(UnboundModel<T> model) where T : Model {
			T imodel = model.Model;
			if (!imodel.WasSetup) {
				Logger.Warn("Model was not setup!");
				return default;
			} else if (CurrentVAO == imodel.VAO) { return imodel; }

			CurrentVAO = imodel.VAO;
			OpenGL4.BindVertexArray(imodel.VAO);
			return imodel;
		}

		/// <summary> Binds the provided texture for use. </summary>
		/// <param name="texture"> The texture to bind and use. </param>
		/// <param name="unit"> Which <see cref="TextureUnit"/> to use. Default is <see cref="TextureUnit.Texture0"/>. </param>
		public static void Bind(Texture texture, TextureUnit unit = TextureUnit.Texture0) {
			if (!texture.WasSetup) {
				Logger.Warn("Texture was not setup!");
				return;
			} else if (CurrentTexture == texture.Handle) { return; }

			CurrentTexture = texture.Handle;
			OpenGL4.ActiveTexture(unit);
			OpenGL4.BindTexture(TextureTarget.Texture2D, texture.Handle);
		}

		/// <summary> Unbinds the current shader. </summary>
		/// <param name="force"> Whether or not to ignore any checks and force a shader unbind. </param>
		public static void UnbindShader(bool force = false) {
			if (CurrentShader == 0 && !force) { return; }

			CurrentShader = 0;
			OpenGL4.UseProgram(0);
		}

		/// <summary> Unbinds the current VAO. </summary>
		/// <param name="force"> Whether or not to ignore any checks and force a VAO unbind. </param>
		public static void UnbindVAO(bool force = false) {
			if (CurrentVAO == 0 && !force) { return; }

			CurrentVAO = 0;
			OpenGL4.BindVertexArray(0);
		}

		/// <summary> Unbinds the current texture. </summary>
		/// <param name="force"> Whether or not to ignore any checks and force a texture unbind. </param>
		public static void UnbindTexture(bool force = false) {
			if (CurrentTexture == 0 && !force) { return; }

			CurrentTexture = 0;
			OpenGL4.BindTexture(TextureTarget.Texture2D, 0);
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