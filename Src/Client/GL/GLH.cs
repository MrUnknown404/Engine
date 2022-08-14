using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine.Client.Font;
using USharpLibs.Engine.Client.GL.Mesh;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	public static class GLH {
		public static int CurrentShader { get; private set; }
		public static int CurrentVAO { get; private set; }
		public static int CurrentTexture { get; private set; }

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
		public static T? Bind<T>(UnboundIMesh<T> mesh) where T : RawMesh {
			T imesh = mesh.IMesh;
			if (!imesh.WasSetup) {
				ClientBase.Logger.WarnLine("Mesh was not setup!");
				return default;
			} else if (CurrentVAO == imesh.VAO) { return imesh; }

			CurrentVAO = imesh.VAO;
			OpenGL4.BindVertexArray(imesh.VAO);
			return imesh;
		}

		public static void Bind(RawTexture texture, TextureUnit unit) {
			if (CurrentTexture == texture.Handle) { return; }

			CurrentTexture = texture.Handle;
			OpenGL4.ActiveTexture(unit);
			OpenGL4.BindTexture(TextureTarget.Texture2D, texture.Handle);
		}

		public static void Bind(DynamicFont font) => Bind(font.Texture, TextureUnit.Texture0);

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
	}
}