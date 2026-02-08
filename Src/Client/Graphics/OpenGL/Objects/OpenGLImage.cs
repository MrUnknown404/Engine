using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using StbiSharp;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	public sealed unsafe class OpenGLImage : NamedGraphicsResource<OpenGLImage, nint> {
		public TextureHandle TextureHandle { get; }

		protected override nint Handle => TextureHandle.Handle;

		internal OpenGLImage(string debugName, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV) : base(debugName) {
			TextureHandle = new(GL.CreateTexture(TextureTarget.Texture2d));

			SetParameteri(TextureParameterName.TextureMinFilter, minFilter);
			SetParameteri(TextureParameterName.TextureMagFilter, magFilter);
			SetParameteri(TextureParameterName.TextureWrapS, wrapModeU);
			SetParameteri(TextureParameterName.TextureWrapT, wrapModeV);

			PrintCreate();

			return;

			void SetParameteri(TextureParameterName parameterName, Enum value) {
				GL.TextureParameteri((int)TextureHandle, parameterName, Convert.ToInt32(value)); // Enum implements IConvertible so this should be safe?
			}
		}

		public void Copy(StbiImage stbiImage, SizedInternalFormat sizeFormat = SizedInternalFormat.Rgba8, PixelFormat pixelFormat = PixelFormat.Rgba) {
			int width = stbiImage.Width;
			int height = stbiImage.Height;
			GL.TextureStorage2D((int)TextureHandle, 1, sizeFormat, width, height); // TODO move to constructor?
			GL.TextureSubImage2D((int)TextureHandle, 0, 0, 0, width, height, pixelFormat, PixelType.UnsignedByte, stbiImage.Data);
		}

		public void Copy(void* data, uint width, uint height, SizedInternalFormat sizeFormat = SizedInternalFormat.Rgba8, PixelFormat pixelFormat = PixelFormat.Rgba) {
			GL.TextureStorage2D((int)TextureHandle, 1, sizeFormat, (int)width, (int)height); // TODO move to constructor?
			GL.TextureSubImage2D((int)TextureHandle, 0, 0, 0, (int)width, (int)height, pixelFormat, PixelType.UnsignedByte, data);
		}

		protected override void Cleanup() => GL.DeleteTexture((int)TextureHandle);
	}
}