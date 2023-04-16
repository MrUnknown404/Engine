using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client.Font;
using USharpLibs.Engine.Client.GL.Model;
using USharpLibs.Engine.Client.GL.OldMesh;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public static class GLH {
		public static int CurrentShader { get; private set; }
		public static int CurrentVAO { get; private set; }
		public static int CurrentTexture { get; private set; }
		private static bool inWireframe, isDepthTesting, isCulling;

		/// <summary> Binds the provided shader for use. </summary>
		/// <typeparam name="T"> The type of shader that is in use. </typeparam>
		/// <param name="shader"> An unbound shader to bind and use. </param>
		/// <param name="use"> An Action to run using the bound shader. </param>
		public static void Bind<T>(UnboundShader<T> shader, Action<T> use) where T : RawShader {
			T s = shader.Shader;
			if (CurrentShader == s.Handle) {
				use(s);
				return;
			}

			CurrentShader = s.Handle;
			OpenGL4.UseProgram(s.Handle);
			use(s);
		}

		/// <summary> Binds the provided mesh for use. </summary>
		/// <typeparam name="T"> The type of mesh that will be returned. </typeparam>
		/// <param name="mesh"> An unbound mesh to bind and use. </param>
		/// <returns> A bound mesh object if everything was successful. If an error occurred null will be returned. </returns>
		[Obsolete("Going to be removed.")]
		[MustUseReturnValue]
		public static T? Bind<T>(UnboundIMesh<T> mesh) where T : IRawMesh {
			T iMesh = mesh.IMesh;
			if (!iMesh.WasSetup) {
				Logger.Warn("Mesh was not setup!");
				return default;
			} else if (CurrentVAO == iMesh.VAO) { return iMesh; }

			CurrentVAO = iMesh.VAO;
			OpenGL4.BindVertexArray(iMesh.VAO);
			return iMesh;
		}

		/// <summary> Binds the provided model for use. </summary>
		/// <typeparam name="T"> The type of model that will be returned. </typeparam>
		/// <param name="model"> An unbound model to bind and use. </param>
		/// <returns> A bound model object if everything was successful. If an error occurred null will be returned. </returns>
		[MustUseReturnValue]
		public static T? Bind<T>(UnboundModel<T> model) where T : RawModel {
			T imodel = model.Model;
			if (!imodel.WasSetup) {
				Logger.Warn("Model was not setup!");
				return default;
			} else if (CurrentVAO == imodel.VAO) { return imodel; }

			CurrentVAO = imodel.VAO;
			OpenGL4.BindVertexArray(imodel.VAO);
			return imodel;
		}

		public static void Bind(RawTexture texture, TextureUnit unit = TextureUnit.Texture0) {
			if (CurrentTexture == texture.Handle) { return; }

			CurrentTexture = texture.Handle;
			OpenGL4.ActiveTexture(unit);
			OpenGL4.BindTexture(TextureTarget.Texture2D, texture.Handle);
		}

		public static void Bind(DynamicFont font) => Bind(font.Texture);

		public static void UnbindShader(bool force = false) {
			if (CurrentShader == 0 && !force) { return; }

			CurrentShader = 0;
			OpenGL4.UseProgram(0);
		}

		public static void UnbindVAO() {
			if (CurrentVAO == 0) { return; }

			CurrentVAO = 0;
			OpenGL4.BindVertexArray(0);
		}

		public static void UnbindTexture() {
			if (CurrentTexture == 0) { return; }

			CurrentTexture = 0;
			OpenGL4.BindTexture(TextureTarget.Texture2D, 0);
		}

		/// <summary> Toggles Wireframe mode. </summary>
		[SuppressMessage("ReSharper", "AssignmentInConditionalExpression")]
		public static void ToggleWireframe() => OpenGL4.PolygonMode(MaterialFace.FrontAndBack, (inWireframe = !inWireframe) ? PolygonMode.Line : PolygonMode.Fill);

		/// <summary> Enables Depth Testing. </summary>
		public static void EnableDepthTest() {
			if (!isDepthTesting) {
				OpenGL4.Enable(EnableCap.DepthTest);
				isDepthTesting = true;
			}
		}

		/// <summary> Disables Depth Testing. </summary>
		public static void DisableDepthTest() {
			if (isDepthTesting) {
				OpenGL4.Disable(EnableCap.DepthTest);
				isDepthTesting = false;
			}
		}

		/// <summary> Enables Culling. </summary>
		public static void EnableCulling() {
			if (!isCulling) {
				OpenGL4.Enable(EnableCap.CullFace);
				isCulling = true;
			}
		}

		/// <summary> Disables Culling. </summary>
		public static void DisableCulling() {
			if (isCulling) {
				OpenGL4.Disable(EnableCap.CullFace);
				isCulling = false;
			}
		}
	}
}