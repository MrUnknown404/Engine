using OpenTK.Mathematics;
using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Init {
	public static class DefaultShaders {
		internal static HashSet<IUnboundShader> AllShaders { get; } = new();

		public const bool DefaultDrawFont = true;
		public const bool DefaultDrawOutline = true;
		public const byte DefaultOutlineSize = 150;
		public static Color4 DefaultFontColor => Color4.White;
		public static Color4 DefaultOutlineColor => Color4.Black;

		public static UnboundShader<DefaultHudShader> DefaultHud { get; } = Add(new DefaultHudShader("DefaultHud") { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });
		public static UnboundShader<DefaultHudShader> DefaultFont { get; } = Add(new DefaultHudShader("DefaultFont") { AssemblyOverride = GameEngine.USharpEngineAssembly.Value, });

		private static UnboundShader<T> Add<T>(T shader) where T : Shader {
			UnboundShader<T> s = new(shader);
			AllShaders.Add(s);
			return s;
		}
	}
}