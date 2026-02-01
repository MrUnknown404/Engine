using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using StbiSharp;

namespace Engine3.Client.Graphics.OpenGL.Objects {
	public class OpenGLImage : INamedGraphicsResource, IEquatable<OpenGLImage> { // TODO untested
		public TextureHandle Handle { get; }

		public string DebugName { get; }
		public bool WasDestroyed { get; private set; }

		internal OpenGLImage(string debugName, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapModeU, TextureWrapMode wrapModeV) {
			DebugName = debugName;
			Handle = new(GL.CreateTexture(TextureTarget.Texture2d));

			SetParameteri(TextureParameterName.TextureMinFilter, minFilter);
			SetParameteri(TextureParameterName.TextureMagFilter, magFilter);
			SetParameteri(TextureParameterName.TextureWrapS, wrapModeU);
			SetParameteri(TextureParameterName.TextureWrapT, wrapModeV);

			INamedGraphicsResource.PrintNameWithHandle(this, Handle.Handle);

			return;

			void SetParameteri(TextureParameterName parameterName, Enum value) {
				GL.TextureParameteri((int)Handle, parameterName, Convert.ToInt32(value)); // Enum implements IConvertible so this should be safe?
			}
		}

		public void Copy(StbiImage stbiImage, SizedInternalFormat sizeFormat = SizedInternalFormat.Rgba8, PixelFormat pixelFormat = PixelFormat.Rgba) {
			int width = stbiImage.Width;
			int height = stbiImage.Height;
			GL.TextureStorage2D((int)Handle, 1, sizeFormat, width, height);
			GL.TextureSubImage2D((int)Handle, 0, 0, 0, width, height, pixelFormat, PixelType.UnsignedByte, stbiImage.Data);
		}

		public void Destroy() {
			if (INamedGraphicsResource.WarnIfDestroyed(this)) { return; }

			GL.DeleteTexture((int)Handle);

			WasDestroyed = true;
		}

		public bool Equals(OpenGLImage? other) => other != null && Handle == other.Handle;
		public override bool Equals(object? obj) => obj is OpenGLImage image && Equals(image);

		public override int GetHashCode() => Handle.GetHashCode();

		public static bool operator ==(OpenGLImage? left, OpenGLImage? right) => Equals(left, right);
		public static bool operator !=(OpenGLImage? left, OpenGLImage? right) => !Equals(left, right);
	}
}