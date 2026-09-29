using System.Reflection;
using JetBrains.Annotations;

namespace Engine4.Utility;

[PublicAPI]
public static class AssetH {
	[MustDisposeResource]
	public static Stream? GetAssetStream(string path, Assembly assembly) => assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}");
}