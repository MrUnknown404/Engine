using System.Reflection;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using StbiSharp;

namespace Engine3.Utility {
	[PublicAPI]
	public static class AssetH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[MustDisposeResource] public static Stream? GetAssetStream(string path, Assembly assembly) => assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}");

		[MustDisposeResource]
		public static StbiImage LoadImage(string fileLocation, string fileExtension, byte texChannels, Assembly assembly) {
			string fullFileName = $"{fileLocation}.{fileExtension}";
			using Stream? textureStream = GetAssetStream($"Textures.{fullFileName}", assembly);
			if (textureStream == null) { throw new Engine3Exception($"Failed to create asset stream at Textures.{fullFileName}"); } // TODO handle null. return default asset

			byte[] data = new byte[textureStream.Length];
			return textureStream.Read(data, 0, data.Length) != data.Length ? throw new Engine3Exception("Texture stream size is not correct") : Stbi.LoadFromMemory(data, texChannels);
		}
	}
}