using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.GL.Models;
using USharpLibs.Engine.Client.GL.Models.Vertex;

namespace USharpLibs.Engine.Client.Fonts {
	[PublicAPI]
	public abstract class Font {
		public Texture FontTexture { get; protected set; } = default!;

		public string Name { get; }
		public byte FontSize { get; }
		public byte Padding { get; }

		protected Font(string name, byte fontSize, byte padding) {
			Name = name;
			FontSize = fontSize;
			Padding = padding;
		}

		/// <summary> Called at the start once the OpenGL context is created. </summary>
		/// <returns> The font's texture atlas for later use. </returns>
		protected internal abstract Texture? Setup();

		/// <param name="text"> The text that should be used for mesh calculations. </param>
		/// <param name="sizeOffset"> The amount of extra space to add to the mesh to account for overdraw.  </param>
		/// <param name="wordWrap"> At what pixel to word wrap. Set to 0 to disable. </param>
		/// <param name="z"> The depth of the meshes. </param>
		public abstract List<Mesh<Vertex5>>GetMesh(string text, float sizeOffset, float wordWrap = 0, float z = 0);

		/// <param name="text"> The text to calculate from. </param>
		/// <returns> The width of the given text in pixels </returns>
		public abstract float GetWidth(string text);
	}
}