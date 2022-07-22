using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine.Client.GL {
	public abstract class RawTexture {
		public int Handle { get; protected set; }
		protected TextureMinFilter MinFilter { get; }
		protected TextureMagFilter MagFilter { get; }
		protected TextureWrapMode WrapMode { get; }
		protected string Name { get; }
		protected bool GenMipMap { get; }

		protected RawTexture(string name, TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool genMipMap = true) {
			Name = name;
			MinFilter = minFilter;
			MagFilter = magFilter;
			GenMipMap = genMipMap;
			WrapMode = wrapMode;
		}

		internal void SetupGL() {
			if (ClientBase.LoadState != LoadState.GL) { throw new Exception($"Cannot setup texture during {ClientBase.LoadState}"); }
			ISetupGL();
		}

		protected abstract void ISetupGL();
	}
}