using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Engine2.Debug;
using USharpLibs.Engine2.Utils;
using ShaderTypeTuple = (string name, OpenTK.Graphics.OpenGL4.ShaderType type);

namespace USharpLibs.Engine2.Client.Shaders {
	public abstract class Shader {
		private const byte ShaderTypeCount = 5;

		public string DebugName { get; }

		internal Dictionary<string, int> UniformLocations { get; } = new();
		internal uint Handle { get; private set; }
		private Assembly Assembly { get; }
		private string?[] FileNames { get; } = new string?[ShaderTypeCount];

		private Shader(string debugName, Assembly? assembly = null) {
			DebugName = debugName;
			Assembly = assembly ?? GameEngine.InstanceSource.Assembly; // TODO load assembly later so we can call this earlier
		}

		internal Shader(string debugName, string fileName, ShaderTypes shaderTypes, Assembly? assembly = null) : this(debugName, assembly) {
			if (shaderTypes == 0) { throw ShaderErrorHandler.CreateException(new(this, ShaderErrorHandler.Reason.EmptyShaderTypes)); }
			for (int i = 0; i < ShaderTypeCount; i++) { FileNames[i] = ((byte)shaderTypes & (1 << i)) != 0 ? fileName : null; }
		}

		internal Shader(string debugName, Assembly? assembly = null, params ShaderTypeTuple[] shaderTypes) : this(debugName, assembly) {
			if (shaderTypes.Length == 0) { throw ShaderErrorHandler.CreateException(new(this, ShaderErrorHandler.Reason.EmptyShaderTypes)); }
			foreach (ShaderTypeTuple shaderType in shaderTypes) { FileNames[ToIndex(shaderType.type)] = shaderType.name; }
		}

		internal void SetupGL() {
			Handle = (uint)GL.CreateProgram();
			int[] shaders = new int[ShaderTypeCount];

			for (int i = 0; i < ShaderTypeCount; i++) {
				string? name = FileNames[i];
				if (!string.IsNullOrEmpty(name)) {
					shaders[i] = CompileShader(name, ToOpenTKShader((ShaderTypes)(1 << i))); // ew. im tired
					GL.AttachShader((int)Handle, shaders[i]);
				}
			}

			GL.LinkProgram(Handle);
			GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int code);
			if (code != (int)All.True) { throw ShaderErrorHandler.CreateException(new(this, ShaderErrorHandler.Reason.LinkError)); }

			foreach (int shader in shaders) {
				if (shader == 0) { continue; } // oops. forgot this
				GL.DetachShader((int)Handle, shader);
				GL.DeleteShader(shader);
			}

			GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);
			for (int i = 0; i < numberOfUniforms; i++) {
				string key = GL.GetActiveUniform((int)Handle, i, out _, out _);
				UniformLocations.Add(key, GL.GetUniformLocation(Handle, key));
			}
		}

		private int CompileShader(string shaderName, ShaderType type) {
			string result;
			using (Stream stream = AssetH.GetAssetStream($"Shaders.{shaderName}.{ToFileFormat(type)}", Assembly)) {
				using (StreamReader reader = new(stream)) { result = reader.ReadToEnd(); }
			}

			int shader = GL.CreateShader(type);
			GL.ShaderSource(shader, result);
			GL.CompileShader(shader);
			GL.GetShader(shader, ShaderParameter.CompileStatus, out int code);
			if (code != (int)All.True) {
				Logger.Error($"Error occurred whilst compiling Shader: {shaderName}.\n\n{GL.GetShaderInfoLog(shader)}");
				throw ShaderErrorHandler.CreateException(new(this, ShaderErrorHandler.Reason.CompileError));
			}

			return shader;
		}

		[SuppressMessage("ReSharper", "PatternIsRedundant")]
		private static string ToFileFormat(ShaderType self) =>
				self switch {
						ShaderType.VertexShader => "vert",
						ShaderType.TessControlShader => "tesc",
						ShaderType.TessEvaluationShader => "tese",
						ShaderType.GeometryShader => "geom",
						ShaderType.FragmentShader => "frag",
						ShaderType.ComputeShader or _ => throw new NotImplementedException(),
				};

		private static ShaderType ToOpenTKShader(ShaderTypes self) =>
				self switch {
						ShaderTypes.Vertex => ShaderType.VertexShader,
						ShaderTypes.TesselationControl => ShaderType.TessControlShader,
						ShaderTypes.TesselationEvaluation => ShaderType.TessEvaluationShader,
						ShaderTypes.Geometry => ShaderType.GeometryShader,
						ShaderTypes.Fragment => ShaderType.FragmentShader,
						_ => throw new NotImplementedException(),
				};

		[SuppressMessage("ReSharper", "PatternIsRedundant")]
		private static byte ToIndex(ShaderType self) =>
				self switch {
						ShaderType.VertexShader => 0,
						ShaderType.TessControlShader => 1,
						ShaderType.TessEvaluationShader => 2,
						ShaderType.GeometryShader => 3,
						ShaderType.FragmentShader => 4,
						ShaderType.ComputeShader or _ => throw new NotImplementedException(),
				};
	}

	[PublicAPI]
	public sealed class Shader<T> : Shader where T : ShaderAccess, new() {
		internal T Access { get; }

		public Shader(string debugName, string fileName, ShaderTypes shaderTypes, Assembly? assembly = null) : base(debugName, fileName, shaderTypes, assembly) => Access = new() { Shader = this, };
		public Shader(string debugName, Assembly? assembly = null, params ShaderTypeTuple[] shaderTypes) : base(debugName, assembly, shaderTypes) => Access = new() { Shader = this, };
	}
}