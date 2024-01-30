using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine.Client.GL {
	public abstract class Texture {
		public int Handle { get; protected set; }
		public bool WasSetup { get; private set; }

		protected TextureMinFilter MinFilter { get; }
		protected TextureMagFilter MagFilter { get; }
		protected TextureWrapMode WrapMode { get; }
		protected bool GenMipMap { get; }

		public ushort Width { get; protected set; }
		public ushort Height { get; protected set; }

		protected Texture(TextureMinFilter minFilter, TextureMagFilter magFilter, TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool genMipMap = true) {
			MinFilter = minFilter;
			MagFilter = magFilter;
			GenMipMap = genMipMap;
			WrapMode = wrapMode;
		}

		internal void SetupGL() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new($"Cannot setup texture during {GameEngine.CurrentLoadState}"); }
			ISetupGL();
			WasSetup = true;
		}

		protected abstract void ISetupGL();
	}
}