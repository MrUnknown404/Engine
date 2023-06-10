using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Client.Fonts {
	public abstract class RawFont {
		public Texture FontTexture { get; protected set; } = default!;

		/// <summary> Called at the start once the OpenGL context is created. </summary>
		/// <returns> The font's texture atlas for later use. </returns>
		protected internal abstract Texture? Setup();
	}
}