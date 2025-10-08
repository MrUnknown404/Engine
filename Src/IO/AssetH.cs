using System.Reflection;
using JetBrains.Annotations;
using NLog;

namespace Engine3.IO {
	[PublicAPI]
	public static class AssetH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[MustDisposeResource]
		public static Stream? GetAssetStream(string path, Assembly assembly) {
			try { return assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}"); }
			catch {
				Logger.Error($"Failed to load asset '{path}' in assembly '{assembly.GetName().Name}'");
				return null;
			}
		}
	}
}