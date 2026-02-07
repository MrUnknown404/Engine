using System.Numerics;
using System.Reflection;
using Engine3.Exceptions;
using Engine3.Utility;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	[PublicAPI]
	public sealed class OpenGLShader : NamedGraphicsResource<OpenGLShader, nint> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public ShaderHandle ShaderHandle { get; }
		public ShaderType ShaderType { get; }

		protected override nint Handle => ShaderHandle.Handle;

		private readonly Dictionary<string, int> uniformLocations = new();

		internal OpenGLShader(string debugName, string fileLocation, ShaderType shaderType, Assembly assembly) : base(debugName) {
			ShaderType = shaderType;

			string fullFileName = $"{GraphicsBackend.OpenGL}.{fileLocation}.{shaderType.FileExtension}.{ShaderLanguage.Glsl.FileExtension}"; // TODO add spirv support https://wikis.khronos.org/opengl/SPIR-V

			using (Stream? shaderStream = AssetH.GetAssetStream($"Shaders.{fullFileName}", assembly)) {
				if (shaderStream == null) { throw new Engine3Exception($"Failed to create asset stream at Shaders.{fullFileName}"); }

				using (StreamReader reader = new(shaderStream)) {
					ShaderHandle = new(GL.CreateShaderProgram(ShaderType switch {
							ShaderType.Fragment => GlShaderType.FragmentShader,
							ShaderType.Vertex => GlShaderType.VertexShader,
							ShaderType.Geometry => GlShaderType.GeometryShader,
							ShaderType.TessEvaluation => GlShaderType.TessEvaluationShader,
							ShaderType.TessControl => GlShaderType.TessControlShader,
							ShaderType.Compute => GlShaderType.ComputeShader,
							_ => throw new ArgumentOutOfRangeException(),
					}, reader.ReadToEnd()));
				}
			}

			GL.GetProgrami((int)ShaderHandle, ProgramProperty.LinkStatus, out int status);
			if ((GlBool)status != GlBool.True) {
				GL.GetProgramInfoLog((int)ShaderHandle, out string info);
				throw new OpenGLException(OpenGLException.Reason.ShaderCompileFail, fullFileName, info);
			}

			GL.GetProgrami((int)ShaderHandle, ProgramProperty.ActiveUniforms, out int uniforms);
			GL.GetProgrami((int)ShaderHandle, ProgramProperty.ActiveUniformMaxLength, out int uniformMaxLength);

			for (uint i = 0; i < uniforms; i++) {
				string name = GL.GetActiveUniform((int)ShaderHandle, i, uniformMaxLength, out int _, out int _, out UniformType _);
				uniformLocations.Add(name, GL.GetUniformLocation((int)ShaderHandle, name));
			}

			PrintCreate();
		}

		private bool CheckForUniform(string name, out int uniformLocation) {
			if (!uniformLocations.TryGetValue(name, out uniformLocation)) {
				Logger.Warn($"Tried to set variable named '{name}' in shader '{DebugName}' but it doesn't exist!");
				return true;
			}

			return false;
		}

		public void SetUniform(string name, bool value) {
			if (CheckForUniform(name, out int uniformLocation)) { return; }
			GL.ProgramUniform1i((int)ShaderHandle, uniformLocation, (int)(value ? GlBool.True : GlBool.False));
		}

		public void SetUniform(string name, int value) {
			if (CheckForUniform(name, out int uniformLocation)) { return; }
			GL.ProgramUniform1i((int)ShaderHandle, uniformLocation, value);
		}

		public void SetUniform(string name, float value) {
			if (CheckForUniform(name, out int uniformLocation)) { return; }
			GL.ProgramUniform1f((int)ShaderHandle, uniformLocation, value);
		}

		public void SetUniform(string name, Vector2 value) {
			if (CheckForUniform(name, out int uniformLocation)) { return; }
			GL.ProgramUniform2f((int)ShaderHandle, uniformLocation, value.X, value.Y);
		}

		public void SetUniform(string name, Vector3 value) {
			if (CheckForUniform(name, out int uniformLocation)) { return; }
			GL.ProgramUniform3f((int)ShaderHandle, uniformLocation, value.X, value.Y, value.Z);
		}

		public void SetUniform(string name, Vector4 value) {
			if (CheckForUniform(name, out int uniformLocation)) { return; }
			GL.ProgramUniform4f((int)ShaderHandle, uniformLocation, value.X, value.Y, value.Z, value.W);
		}

		public void SetUniform(string name, Matrix4x4 value, bool transpose = false) {
			if (CheckForUniform(name, out int uniformLocation)) { return; }
			GL.ProgramUniformMatrix4f((int)ShaderHandle, uniformLocation, 1, transpose, in value);
		}

		protected override void Cleanup() => GL.DeleteProgram((int)ShaderHandle);
	}
}