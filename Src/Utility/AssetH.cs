using System.Reflection;
using Engine3.Client.Graphics;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using SharpGLTF.Runtime;
using SharpGLTF.Schema2;
using StbiSharp;
using Material = SharpGLTF.Schema2.Material;

namespace Engine3.Utility {
	[PublicAPI]
	public static class AssetH {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[MustDisposeResource] public static Stream? GetAssetStream(string path, Assembly assembly) => assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}");

		[MustDisposeResource]
		public static StbiImage LoadImage(string fileLocation, string fileExtension, byte texChannels, Assembly assembly) {
			const string MissingTextureName = "Missing.png";

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

		[MustUseReturnValue]
		public static RenderMesh LoadMesh(string fileLocation, [RequireStaticDelegate] GLTFMeshesToMeshDelegate gltfMeshesToMesh, Assembly assembly, GLTFFileType fileType = GLTFFileType.GLB) {
			string fullFileName = $"{fileLocation}.{fileType.ToString().ToLower()}";
			using Stream? modelStream = GetAssetStream($"Models.{fullFileName}", assembly);

			if (modelStream == null) { throw new Engine3Exception($"Failed to create asset stream at: Models.{fullFileName}"); }

			ModelRoot modelRoot = ModelRoot.ReadGLB(modelStream); // more later
			return gltfMeshesToMesh(modelRoot.LogicalMeshes);
		}

		[MustUseReturnValue]
		public static Model LoadModel(string fileLocation, [RequireStaticDelegate] GLTFMeshToMeshDelegate gltfMeshToMesh, Assembly assembly, byte vertexStride, GLTFFileType fileType = GLTFFileType.GLB) {
			string fullFileName = $"{fileLocation}.{fileType.ToString().ToLower()}";
			using Stream? modelStream = GetAssetStream($"Models.{fullFileName}", assembly);

			if (modelStream == null) { throw new Engine3Exception($"Failed to create asset stream at: Models.{fullFileName}"); }

			ModelRoot modelRoot = ModelRoot.ReadGLB(modelStream);
			IMeshDecoder<Material>[] meshDecoders = modelRoot.LogicalMeshes.Decode();

			return new(meshDecoders.Select(static meshDecoder => meshDecoder.Primitives).SelectMany(pList => pList.Select(primitiveDecoder => gltfMeshToMesh(primitiveDecoder))).ToArray().ToArray(), vertexStride);
		}

		public delegate RenderMesh GLTFMeshesToMeshDelegate(IReadOnlyList<Mesh> gltfMesh);
		public delegate RenderMesh GLTFMeshToMeshDelegate(IMeshPrimitiveDecoder<Material> gltfMesh);

		public enum GLTFFileType {
			GLTF,
			GLB,
		}
	}
}