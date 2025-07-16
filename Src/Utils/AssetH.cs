using System.Reflection;

namespace USharpLibs.Engine.Utils {
	public static class AssetH {
		public static Stream GetAssetStream(string path, Assembly assembly) {
			string streamName = $"{assembly.GetName().Name}.Assets.{path}";

			if (assembly.GetManifestResourceStream(streamName) is { } stream) {
				return stream; //
			} else { throw new NullReferenceException($"Could not find file '{path}' in assembly '{assembly.GetName().Name}'"); }
		}
	}
}