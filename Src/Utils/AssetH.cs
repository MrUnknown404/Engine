using System.Reflection;
using JetBrains.Annotations;
using NLog;

namespace Engine3.Utils {
	[PublicAPI]
	public static class AssetH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[MustDisposeResource]
		public static Stream? GetAssetStream(string path, Assembly assembly) { // TODO handle null. return default asset
			Stream? s = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}");
			if (s == null) { Logger.Error($"Failed to load asset '{path}' in assembly '{assembly.GetName().Name}'"); }
			return s;
		}
	}
}