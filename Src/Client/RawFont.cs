using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Client {
	public abstract class RawFont {
		public Texture FontTexture { get; protected set; } = default!;

		protected internal abstract Texture? Setup();
	}
}