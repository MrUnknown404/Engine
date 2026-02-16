using System.Reflection;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using StbiSharp;

namespace Engine3.Utility {
	[PublicAPI]
	public static class AssetH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string MissingTextureName = "Missing.png";

		[MustDisposeResource] public static Stream? GetAssetStream(string path, Assembly assembly) => assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}");

		[MustDisposeResource]
		public static StbiImage LoadImage(string fileLocation, string fileExtension, byte texChannels, Assembly assembly) {
			string fullFileName = $"{fileLocation}.{fileExtension}";
			Stream? textureStream = GetAssetStream($"Textures.{fullFileName}", assembly);

			if (textureStream == null) {
				Logger.Error($"Failed to create asset stream at: Textures.{fullFileName}");
				textureStream = GetAssetStream($"Textures.{MissingTextureName}", Engine3.Assembly) ?? throw new NullReferenceException();
			}

			byte[] data = new byte[textureStream.Length];
			if (textureStream.Read(data, 0, data.Length) != data.Length) { throw new Engine3Exception("Texture stream size is not correct"); }

			StbiImage image = Stbi.LoadFromMemory(data, texChannels);
			textureStream.Dispose();
			return image;
		}
	}
}