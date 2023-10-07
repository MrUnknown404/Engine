using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	[PublicAPI]
	public class SimpleTexture : Texture {
		public ColorComponents ColorComponent { get; init; } = ColorComponents.RedGreenBlueAlpha;
		public PixelInternalFormat PixelInternalFormat { get; init; } = PixelInternalFormat.Rgba;
		public PixelFormat PixelFormat { get; init; } = PixelFormat.Rgba;

		protected string? Name { get; }
		protected byte[]? Data { get; }
		public int Width { get; }
		public int Height { get; }

		public SimpleTexture(string name, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool genMipMap = true) : base(minFilter, magFilter, wrapMode, genMipMap) =>
				Name = name;

		public SimpleTexture(byte[] data, int width, int height, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool genMipMap = true) : base(minFilter, magFilter,
				wrapMode, genMipMap) {
			Data = data;
			Width = width;
			Height = height;
		}

		protected override void ISetupGL() {
			Handle = OpenGL4.GenTexture();
			OpenGL4.ActiveTexture(TextureUnit.Texture0);
			OpenGL4.BindTexture(TextureTarget.Texture2D, Handle);

			if (Name != null) {
				string streamName = $"{GameEngine.InstanceAssembly.Value.GetName().Name}.Assets.Textures.{Name}.png";
				if (GameEngine.InstanceAssembly.Value.GetManifestResourceStream(streamName) is Stream stream) {
					using (stream) {
						StbImage.stbi_set_flip_vertically_on_load(1);
						ImageResult image = ImageResult.FromStream(stream, ColorComponent);
						OpenGL4.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat, image.Width, image.Height, 0, PixelFormat, PixelType.UnsignedByte, image.Data);
					}
				} else { throw new Exception($"Could not find file '{Name}' at '{streamName}'"); }
			} else if (Data != null) {

				OpenGL4.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat, Width, Height, 0, PixelFormat, PixelType.UnsignedByte, Data);
			} else {
				throw new Exception("Cannot load texture because no data was given! how'd you do that?");
			}

			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)MinFilter);
			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)MagFilter);
			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)WrapMode);
			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)WrapMode);

			if (GenMipMap) { OpenGL4.GenerateMipmap(GenerateMipmapTarget.Texture2D); }

			GLH.UnbindTexture();
		}
	}
}