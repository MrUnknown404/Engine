using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client.GL {
	public class Texture : RawTexture {
		public Texture(string name, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool genMipMap = true) : base(name, minFilter, magFilter, wrapMode, genMipMap) { }

		protected override void ISetupGL() {
			Handle = OpenGL4.GenTexture();
			GLH.Bind(this, TextureUnit.Texture0);

			StbImage.stbi_set_flip_vertically_on_load(1);
			using (Stream stream = File.OpenRead($"Assets/Textures/{Name}.png")) {
				ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
				OpenGL4.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
			}

			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)MinFilter);
			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)MagFilter);
			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)WrapMode);
			OpenGL4.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)WrapMode);

			if (GenMipMap) {
				OpenGL4.GenerateMipmap(GenerateMipmapTarget.Texture2D);
			}

			GLH.UnbindTexture();
		}
	}
}