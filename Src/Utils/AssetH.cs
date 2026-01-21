using System.Reflection;
using JetBrains.Annotations;
using NLog;

namespace Engine3.Utils {
	[PublicAPI]
	public static class AssetH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[MustDisposeResource] public static Stream? GetAssetStream(string path, Assembly assembly) => assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}"); // TODO handle null. return default asset
	}
}