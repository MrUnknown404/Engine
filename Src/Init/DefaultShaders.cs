using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL.Shaders;

namespace USharpLibs.Engine.Init {
	[PublicAPI]
	public static class DefaultShaders {
		internal static HashSet<Shader> AllShaders { get; } = new();

		public static Shader<ShaderWriter> DefaultHud { get; } = Add(Shader("DefaultHud", ShaderTypes.Vertex | ShaderTypes.Fragment));
		public static Shader<FontShaderWriter> DefaultFont { get; } = Add(new Shader<FontShaderWriter>("DefaultFont", ShaderTypes.Vertex | ShaderTypes.Fragment) { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });
		public static Shader<ShaderWriter> NineSlice { get; } = Add(Shader("NineSlice", ShaderTypes.Vertex | ShaderTypes.Fragment));

		private static Shader<ShaderWriter> Shader(string shaderName, ShaderTypes shaderTypes) => new(shaderName, shaderTypes) { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, };

		private static T Add<T>(T shader) where T : Shader {
			AllShaders.Add(shader);
			return shader;
		}
	}
}