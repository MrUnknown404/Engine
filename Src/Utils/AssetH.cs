using System.Reflection;

namespace USharpLibs.Engine2.Utils {
	[PublicAPI]
	public static class AssetH {
		[MustDisposeResource] public static Stream GetAssetStream(string path) => GetAssetStream(path, GameEngine.InstanceSource.Assembly);

		[MustDisposeResource]
		public static Stream GetAssetStream(string path, Assembly assembly) =>
				assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}") ?? throw new NullReferenceException($"Could not find file '{path}' in assembly '{assembly.GetName().Name}'");
	}
}