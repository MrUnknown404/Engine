using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Client.Model.Mesh.Vertex;
using Engine3.Utils;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.OpenGL4;

namespace Engine3.Client {
	[PublicAPI]
	public abstract class Shader {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private Dictionary<string, int> UniformLocations { get; } = new();

		public VertexLayout VertexLayout { get; }
		public string DebugName { get; }
		public bool CreateTessControl { private get; init; }
		public bool CreateTessEval { private get; init; }
		public bool CreateGeometry { private get; init; }
		public Lazy<Assembly> Assembly { private get; init; } = new(static () => GameEngine.InstanceAssembly ?? throw new EngineStateException(EngineStateException.Reason.StartNotCalled));

		internal uint Handle { get; private set; }

		public bool HasHandle => Handle != 0;

		private readonly string fileName;

		protected Shader(string debugName, string fileName, VertexLayout vertexLayout) {
			DebugName = debugName;
			this.fileName = fileName;
			VertexLayout = vertexLayout;
		}

		internal void SetupGL() {
			if (HasHandle) { throw new ShaderException(ShaderException.Reason.HasHandle); }

			Handle = (uint)GL.CreateProgram();

			uint vertexHandle = CompileShader(fileName, ShaderType.VertexShader);
			uint fragHandle = CompileShader(fileName, ShaderType.FragmentShader);
			uint tessCtrlHandle = 0;
			uint tessEvalHandle = 0;
			uint geometryHandle = 0;

			GL.AttachShader(Handle, vertexHandle);
			GL.AttachShader(Handle, fragHandle);

			if (CreateTessControl) {
				tessCtrlHandle = CompileShader(fileName, ShaderType.TessControlShader);
				GL.AttachShader(Handle, tessCtrlHandle);
			}

			if (CreateTessEval) {
				tessEvalHandle = CompileShader(fileName, ShaderType.TessEvaluationShader);
				GL.AttachShader(Handle, tessEvalHandle);
			}

			if (CreateGeometry) {
				geometryHandle = CompileShader(fileName, ShaderType.GeometryShader);
				GL.AttachShader(Handle, geometryHandle);
			}

			GL.LinkProgram(Handle);
			GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int code);
			if (code != (int)All.True) { throw new ShaderException(ShaderException.Reason.FailedToLink); }

			GL.DetachShader(Handle, vertexHandle);
			GL.DeleteShader(vertexHandle);
			GL.DetachShader(Handle, fragHandle);
			GL.DeleteShader(fragHandle);

			if (CreateTessControl) {
				GL.DetachShader(Handle, tessCtrlHandle);
				GL.DeleteShader(tessCtrlHandle);
			}

			if (CreateTessEval) {
				GL.DetachShader(Handle, tessEvalHandle);
				GL.DeleteShader(tessEvalHandle);
			}

			if (CreateGeometry) {
				GL.DetachShader(Handle, geometryHandle);
				GL.DeleteShader(geometryHandle);
			}

			GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);
			for (int i = 0; i < numberOfUniforms; i++) {
				string key = GL.GetActiveUniform((int)Handle, i, out _, out _);
				UniformLocations.Add(key, GL.GetUniformLocation(Handle, key));
			}
		}

		[SuppressMessage("ReSharper", "PatternIsRedundant")]
		private uint CompileShader(string shaderName, ShaderType type) {
			string result;

			using (Stream stream = AssetH.GetAssetStream($"Shaders.{shaderName}.{type switch {
					ShaderType.VertexShader => "vert",
					ShaderType.TessControlShader => "tesc",
					ShaderType.TessEvaluationShader => "tese",
					ShaderType.GeometryShader => "geom",
					ShaderType.FragmentShader => "frag",
					ShaderType.ComputeShader or _ => throw new NotImplementedException(),
			}}", Assembly.Value)) {
				using (StreamReader reader = new(stream)) { result = reader.ReadToEnd(); }
			}

			int shader = GL.CreateShader(type);
			GL.ShaderSource(shader, result);
			GL.CompileShader(shader);

			GL.GetShader(shader, ShaderParameter.CompileStatus, out int code);
			if (code != (int)All.True) {
				Logger.Error($"Error occurred whilst compiling Shader: {shaderName}.\n\n{GL.GetShaderInfoLog(shader)}");
				throw new ShaderException(ShaderException.Reason.FailedToCompile);
			}

			return (uint)shader;
		}

		public bool TryGetUniform(string name, out int location) {
			if (UniformLocations.TryGetValue(name, out location)) { return true; }
			Logger.Warn($"Attempted to set variable that does not exist called '{name}' in shader '{DebugName}'");
			return false;
		}
	}

	[PublicAPI]
	public sealed class Shader<T> : Shader where T : ShaderContext, new() {
		internal T Context { get; }

		public Shader(string debugName, string fileName, VertexLayout vertexLayout) : base(debugName, fileName, vertexLayout) => Context = new T { Shader = this, };
	}
}