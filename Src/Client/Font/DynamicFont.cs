using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Client.Font {
	public class DynamicFont {
		private readonly IFontCharWidth charWidth;
		private readonly Dictionary<char, Glyph> glyphDict = new();
		internal Texture Texture { get; }

		public DynamicFont(string name, IFontCharWidth charWidth, TextureMinFilter minFilter, TextureMagFilter magFilter, bool genMipMap = true) {
			this.charWidth = charWidth;
			Texture = new Texture($"Fonts/{name}_fontmap", minFilter, magFilter, TextureWrapMode.Repeat, genMipMap);
		}

		internal void SetupGL() {
			if (ClientBase.LoadState != LoadState.GL) { throw new Exception($"Cannot setup font during {ClientBase.LoadState}"); }

			float w = 33f / 527f, h = 33f / 197f, h2 = 0.166f; // h2 is 33 / 197 rounded to 0.166. don't ask.
			for (int i = 1; i < 94 + 1; i++) {
				char @char = (char)(0x0020 + i);
				glyphDict[@char] = new(@char, i % 16 * w, -(i / 16 * h2) - h, charWidth.CharWidth(@char) / 527f, h);
			}
		}

		public float GetWidth(string str, float fontSize) {
			float width = 0, spaceSize = 0.03125f * fontSize;
			foreach (char @char in str) {
				if (@char == ' ') {
					width += spaceSize;
					continue;
				}

				width += glyphDict[@char].W * 2 * fontSize;
			}

			return width;
		}

		public Shape ToShape(string str, float fontSize, float z = 0) {
			float curX = 0, spaceSize = 0.03125f * fontSize;
			List<float> verts = new();

			foreach (char @char in str) {
				if (@char == ' ') {
					curX += spaceSize;
					continue;
				}

				Glyph g = glyphDict[@char];
				float gW = g.W * 2 * fontSize;

				verts.AddRange(Quads.XYWH(curX, 0, gW, g.H * fontSize, z, g.X0, g.Y0, g.X1, g.Y1, g.X2, g.Y2, g.X3, g.Y3).Vertices);
				curX += gW;
			}

			return new(verts.ToArray());
		}
	}

	public interface IFontCharWidth {
		public int CharWidth(char @char);
	}
}