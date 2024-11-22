using USharpLibs.Engine2.Client.Shaders;

namespace USharpLibs.Engine2.Init {
	[PublicAPI]
	public static class DefaultShaders {
		internal static HashSet<Shader> AllShaders { get; } = new();

		public static Shader<ShaderAccess> DefaultHud { get; } = RegisterShader<ShaderAccess>(new("DefaultHud", "DefaultHud", ShaderTypes.Vertex | ShaderTypes.Fragment, GameEngine.EngineSource.Assembly));

		private static Shader<T> RegisterShader<T>(Shader<T> shader) where T : ShaderAccess, new() {
			AllShaders.Add(shader);
			return shader;
		}
	}
}