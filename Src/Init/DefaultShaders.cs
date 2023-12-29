using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Init {
	[PublicAPI]
	public static class DefaultShaders {
		internal static HashSet<IUnboundShader> AllShaders { get; } = new();

		public static UnboundShader<DefaultHudShader> DefaultHud { get; } = Add(new DefaultHudShader("DefaultHud") { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });
		public static UnboundShader<DefaultFontShader> DefaultFont { get; } = Add(new DefaultFontShader("DefaultHud", "DefaultFont") { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });
		public static UnboundShader<DefaultHudShader> NineSlice { get; } = Add(new DefaultHudShader("DefaultHud", "NineSlice") { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });

		private static UnboundShader<T> Add<T>(T shader) where T : Shader {
			UnboundShader<T> s = new(shader);
			AllShaders.Add(s);
			return s;
		}
	}
}