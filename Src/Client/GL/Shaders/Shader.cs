using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Utils;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;
using ShaderTypeTuple = (string name, OpenTK.Graphics.OpenGL4.ShaderType type);

namespace USharpLibs.Engine2.Client.GL.Shaders {
	[PublicAPI]
	public abstract class Shader {
		private const byte ShaderTypeCount = 5;

		public string DebugName { get; }

		internal Dictionary<string, int> UniformLocations { get; } = new();
		internal uint Handle { get; private set; }
		private Assembly Assembly { get; }
		private string?[] FileNames { get; } = new string?[ShaderTypeCount];

		private Shader(string debugName, Assembly? assembly = null) {
			DebugName = debugName;
			Assembly = assembly ?? GameEngine.InstanceSource.Assembly;
		}

		internal Shader(string debugName, string fileName, ShaderTypes shaderTypes, Assembly? assembly = null) : this(debugName, assembly) {
			if (shaderTypes == 0) { throw new ArgumentException("ShaderTypes cannot be 0."); }
			for (int i = 0; i < ShaderTypeCount; i++) { FileNames[i] = ((byte)shaderTypes & (1 << i)) != 0 ? fileName : null; }
		}

		internal Shader(string debugName, Assembly? assembly = null, params ShaderTypeTuple[] shaderTypes) : this(debugName, assembly) {
			if (shaderTypes.Length == 0) { throw new ArgumentException("ShaderTypes cannot be empty."); }
			foreach (ShaderTypeTuple shaderType in shaderTypes) { FileNames[shaderType.type.ToIndex()] = shaderType.name; }
		}

		internal void SetupGL() {
			Handle = (uint)OpenGL4.CreateProgram();
			int[] shaders = new int[ShaderTypeCount];

			for (int i = 0; i < ShaderTypeCount; i++) {
				string? name = FileNames[i];
				if (!string.IsNullOrEmpty(name)) {
					shaders[i] = CompileShader(name, ((ShaderTypes)(1 << i)).ToOpenTKShader()); // ew. im tired
					OpenGL4.AttachShader((int)Handle, shaders[i]);
				}
			}

			OpenGL4.LinkProgram(Handle);
			OpenGL4.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int code);
			if (code != (int)All.True) { throw new($"Error occurred whilst linking Shader '{DebugName}' Id:{Handle}"); }

			foreach (int shader in shaders) {
				OpenGL4.DetachShader((int)Handle, shader);
				OpenGL4.DeleteShader(shader);
			}

			OpenGL4.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);
			for (int i = 0; i < numberOfUniforms; i++) {
				string key = OpenGL4.GetActiveUniform((int)Handle, i, out _, out _);
				UniformLocations.Add(key, OpenGL4.GetUniformLocation(Handle, key));
			}
		}

		private int CompileShader(string shaderName, ShaderType type) {
			string result;
			using (Stream stream = AssetH.GetAssetStream($"Shaders.{shaderName}.{type.ToFileFormat()}", Assembly)) {
				using (StreamReader reader = new(stream)) { result = reader.ReadToEnd(); }
			}

			int shader = OpenGL4.CreateShader(type);
			OpenGL4.ShaderSource(shader, result);
			OpenGL4.CompileShader(shader);
			OpenGL4.GetShader(shader, ShaderParameter.CompileStatus, out int code);
			if (code != (int)All.True) { throw new($"Error occurred whilst compiling '{shaderName}' Id:{shader}.\n\n{OpenGL4.GetShaderInfoLog(shader)}"); }
			return shader;
		}
	}

	[PublicAPI]
	public sealed class Shader<T> : Shader where T : ShaderAccess, new() {
		internal T Access { get; }

		public Shader(string debugName, string fileName, ShaderTypes shaderTypes, Assembly? assembly = null) : base(debugName, fileName, shaderTypes, assembly) => Access = new() { Shader = this, };
		public Shader(string debugName, Assembly? assembly = null, params ShaderTypeTuple[] shaderTypes) : base(debugName, assembly, shaderTypes) => Access = new() { Shader = this, };
	}
}