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

		[MustDisposeResource] internal static Stream? GetAssetStream(string path, Assembly assembly) => assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Assets.{path}");

		/// <summary> Loads an embedded image at the given location </summary>
		/// <param name="fileLocation"> Where the image is </param>
		/// <param name="fileType"> What file type the image is </param>
		/// <param name="texChannels"> The amount of color channels the image has </param>
		/// <param name="assembly"> The assembly to search in </param>
		/// <returns> An <see cref="StbiImage"/> containing pixel data. Will return a default texture if <see cref="fileLocation"/> does not point to a valid texture </returns>
		/// <exception cref="NullReferenceException"> Thrown if this could not find the default texture atlas </exception>
		/// <exception cref="Engine3Exception"> Thrown if the input texture stream differs in size from what was expected </exception>
		[MustDisposeResource]
		public static StbiImage LoadImage(string fileLocation, string fileType, byte texChannels, Assembly assembly) {
			const string MissingTextureName = "Missing.png";

			string fullFileName = $"{fileLocation}.{fileType}";
			Stream? textureStream = GetAssetStream($"Textures.{fullFileName}", assembly);

			if (textureStream == null) {
				Logger.Error($"Failed to create asset stream at: Textures.{fullFileName}");
				textureStream = GetAssetStream($"Textures.{MissingTextureName}", Engine3.Assembly) ?? throw new NullReferenceException("Could not find default texture");
			}

			byte[] data = new byte[textureStream.Length];
			if (textureStream.Read(data, 0, data.Length) != data.Length) { throw new Engine3Exception("Texture stream size is not correct"); }

			StbiImage image = Stbi.LoadFromMemory(data, texChannels);
			textureStream.Dispose();
			return image;
		}

		/// <summary> Loads an embedded mesh at the given location </summary>
		/// <param name="fileLocation"> Where the mesh is </param>
		/// <param name="gltfMeshesToMesh"> A function for converting a list of gltf meshes into a single mesh. <see cref="Mesh"/> to <see cref="RenderMesh"/> </param>
		/// <param name="assembly"> The assembly to search in </param>
		/// <param name="fileType"> What file type the mesh is </param>
		/// <returns> A <see cref="RenderMesh"/> containing mesh data read from the file at the provided location. An exception will be thrown if we fail load the mesh </returns>
		/// <exception cref="Engine3Exception"> Thrown if this fails to get or read mesh data </exception>
		[MustUseReturnValue]
		public static RenderMesh LoadMesh(string fileLocation, [RequireStaticDelegate] GLTFMeshesToMeshDelegate gltfMeshesToMesh, Assembly assembly, GLTFFileType fileType = GLTFFileType.GLB) {
			string fullFileName = $"{fileLocation}.{fileType.ToString().ToLower()}";
			using Stream? modelStream = GetAssetStream($"Models.{fullFileName}", assembly);

			if (modelStream == null) { throw new Engine3Exception($"Failed to create asset stream at: Models.{fullFileName}"); }

			ModelRoot modelRoot = ModelRoot.ReadGLB(modelStream); // more later
			return gltfMeshesToMesh(modelRoot.LogicalMeshes);
		}

		/// <summary> Loads an embedded model at the given location </summary>
		/// <param name="fileLocation"> Where the model is </param>
		/// <param name="gltfMeshToMesh"> A function for converting a gltf mesh into our mesh. <see cref="Mesh"/> to <see cref="RenderMesh"/> </param>
		/// <param name="assembly"> The assembly to search in </param>
		/// <param name="vertexStride"> The model's vertex stride </param>
		/// <param name="fileType"> What file type the model is </param>
		/// <returns> A <see cref="Model"/> containing mesh data read from the file at the provided location. An exception will be thrown if we fail load the model </returns>
		/// <exception cref="Engine3Exception"> Thrown if this fails to get or read model data </exception>
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