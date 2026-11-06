using System.Reflection;
using Engine4.IO;
using Engine4.Utility.Exceptions;
using JetBrains.Annotations;
using NLog;
using StbiSharp;

namespace Engine4.Client.Utility;

[PublicAPI]
public static class AssetH {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	[MustDisposeResource]
	public static Stream? GetAssetStream(string path, Assembly assembly) => assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}");

	[MustDisposeResource]
	public static StbiImage LoadImage(string path, string fileType, byte texChannels, Assembly assembly) {
		const string MissingTextureName = "Missing.png";

		string fullFileName = $"{path}.{fileType}";
		Stream? textureStream = GetAssetStream($"Textures.{fullFileName}", assembly);

		if (textureStream == null) {
			Logger.Error($"Failed to create asset stream at: Textures.{fullFileName} in Assembly: {assembly.GetName()}");
			textureStream = GetAssetStream($"Textures.{MissingTextureName}", typeof(AssetH).Assembly) ?? throw new NullReferenceException("Could not find default texture");
		}

		byte[] data = new byte[textureStream.Length];
		if (textureStream.Read(data, 0, data.Length) != data.Length) { throw new Engine4Exception("Texture stream size is not correct"); }

		StbiImage image = Stbi.LoadFromMemory(data, texChannels);
		textureStream.Dispose();

		return image;
	}
}