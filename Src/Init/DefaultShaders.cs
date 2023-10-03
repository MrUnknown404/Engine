using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Init {
	public static class DefaultShaders {
		internal static HashSet<IUnboundShader> AllShaders { get; } = new();

		public static readonly UnboundShader<DefaultHudShader> DefaultHud = Add(new DefaultHudShader("DefaultHud") { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });
		public static readonly UnboundShader<DefaultHudShader> DefaultFont = Add(new DefaultHudShader("DefaultFont") { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });

		private static UnboundShader<T> Add<T>(T shader) where T : Shader {
			UnboundShader<T> s = new(shader);
			AllShaders.Add(s);
			return s;
		}
	}
}