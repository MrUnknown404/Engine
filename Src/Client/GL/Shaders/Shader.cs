using System.Reflection;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using USharpLibs.Engine.Utils;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL.Shaders {
	public abstract class Shader {
		public Assembly? AssemblyOverride { private get; init; }
		protected internal Dictionary<string, int> UniformLocations { get; } = new();
		protected internal int Handle { get; private set; }
		protected internal bool WasSetup { get; private set; }

		internal string ShaderName { get; }
		private ShaderTypes ShaderTypes { get; }

		internal Shader(string shaderName, ShaderTypes shaderTypes) {
			if (shaderTypes == 0) { throw new("ShaderTypes cannot be 0."); }
			ShaderName = shaderName;
			ShaderTypes = shaderTypes;
		}

		internal void SetupGL() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new($"Cannot setup shader during {GameEngine.CurrentLoadState}"); }

			Handle = OpenGL4.CreateProgram();

			ShaderTypes[] types = Enum.GetValues<ShaderTypes>().Where(s => ShaderTypes.HasFlag(s)).ToArray();
			int[] shaders = new int[types.Length];

			for (int i = 0; i < types.Length; i++) {
				CompileShader(types[i], out shaders[i]);
				OpenGL4.AttachShader(Handle, shaders[i]);
			}

			OpenGL4.LinkProgram(Handle);
			OpenGL4.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int code);
			if (code != (int)All.True) { throw new($"Error occurred whilst linking Shader '{ShaderName}' Id:{Handle}"); }

			foreach (int shader in shaders) {
				OpenGL4.DetachShader(Handle, shader);
				OpenGL4.DeleteShader(shader);
			}

			OpenGL4.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int numberOfUniforms);
			for (int i = 0; i < numberOfUniforms; i++) {
				string key = OpenGL4.GetActiveUniform(Handle, i, out _, out _);
				UniformLocations.Add(key, OpenGL4.GetUniformLocation(Handle, key));
			}

			WasSetup = true;
		}

		private void CompileShader(ShaderTypes type, out int shader) {
			Assembly assembly = AssemblyOverride ?? GameEngine.InstanceAssembly.Value;
			string result;

			using (Stream stream = AssetH.GetAssetStream($"Shaders.{ShaderName}.{type.ToFileFormat()}", assembly)) {
				using (StreamReader reader = new(stream)) { result = reader.ReadToEnd(); }
			}

			OpenGL4.ShaderSource(shader = OpenGL4.CreateShader(type.ToOpenTKShader()), result);
			OpenGL4.CompileShader(shader);
			OpenGL4.GetShader(shader, ShaderParameter.CompileStatus, out int code);
			if (code != (int)All.True) { throw new($"Error occurred whilst compiling '{ShaderName}' Id:{shader}.\n\n{OpenGL4.GetShaderInfoLog(shader)}"); }
		}

		internal abstract void InvokeOnResize(ResizeEventArgs args);
	}

	[PublicAPI]
	public sealed class Shader<T> : Shader where T : ShaderWriter, new() {
		public Action<Shader, T, ResizeEventArgs>? OnResize { private get; init; } = (_, access, args) => access.SetProjection(Matrix4.CreateOrthographicOffCenter(0, args.Width, args.Height, 0, -10, 10));

		public Shader(string shaderName, ShaderTypes shaderTypes) : base(shaderName, shaderTypes) { }

		internal override void InvokeOnResize(ResizeEventArgs args) {
			using (T access = GLH.Bind(this)) { OnResize?.Invoke(this, access, args); }
		}
	}

	[Flags]
	public enum ShaderTypes : byte {
		Vertex = 1 << 0,
		TesselationControl = 1 << 1,
		TesselationEvaluation = 1 << 2,
		Geometry = 1 << 3,
		Fragment = 1 << 4,
		//Compute = 1 << 5, // I have no idea if this is related to what i want to use shaders for
	}
}