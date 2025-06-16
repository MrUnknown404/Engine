using System.Reflection;
using JetBrains.Annotations;

namespace Engine3.Utils {
	[PublicAPI]
	public static class AssetH {
		[MustDisposeResource] public static Stream GetAssetStream(Assembly assembly, string path) => GetAssetStream(path, assembly);

		[MustDisposeResource]
		public static Stream GetAssetStream(string path, Assembly assembly) =>
				assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}") ?? throw new NullReferenceException($"Failed to load asset '{path}' in assembly '{assembly.GetName().Name}'");
	}
}