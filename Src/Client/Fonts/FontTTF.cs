using System.Reflection;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using FreeTypeSharp.Native;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Common.Math;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.GL.Models;
using USharpLibs.Engine.Client.GL.Shapes2D;

namespace USharpLibs.Engine.Client.Fonts {
	[PublicAPI]
	public sealed class FontTTF : Font {
		private const byte AtlasGridSize = 10;

		public Assembly? AssemblyOverride { get; init; }

		private Dictionary<char, Glyph> GlyphMap { get; } = new();
		private FreeTypeFaceFacade? fontFacade;

		private ushort biggestGlyph, spaceSize;

		public FontTTF(string name, byte fontSize, byte padding) : base(name, fontSize, padding) { }

		protected internal override Texture Setup() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new($"Cannot setup fonts during {GameEngine.CurrentLoadState}"); }

			Assembly assembly = AssemblyOverride ?? GameEngine.InstanceAssembly.Value;
			string streamName = $"{assembly.GetName().Name}.Assets.Fonts.{Name}.ttf";

			// Load font and FreeType
			if (assembly.GetManifestResourceStream(streamName) is { } stream) {
				using (stream) {
					int fontStreamLength = (int)stream.Length;
					byte[] fontData = new byte[fontStreamLength];

					// ReSharper disable once MustUseReturnValue
					stream.Read(fontData, 0, fontStreamLength);
					nint data = Marshal.AllocCoTaskMem(fontStreamLength);
					Marshal.Copy(fontData, 0, data, fontStreamLength);

					(fontFacade = new(new(), data, fontStreamLength)).SelectCharSize(FontSize, 96, 96);
				}
			} else { throw new($"Could not find file '{Name}' at '{streamName}'"); }

			FT_Error error;

			// First calculate biggest glyph. FreeType might do this for me? idk.
			for (int i = 33; i < 127; i++) {
				char c = (char)i;

				error = FT.FT_Load_Char(fontFacade.Face, c, FT.FT_LOAD_DEFAULT);
				if (error != FT_Error.FT_Err_Ok) { throw new($"Error loading font. Error code: {error}"); }

				ushort big = (ushort)System.Math.Max(fontFacade.GlyphMetricWidth, fontFacade.GlyphMetricHeight);
				if (big > biggestGlyph) { biggestGlyph = big; }
			}

			// Figure out image size then create a Bitmap pixel array
			ushort imageSize = (ushort)MathH.Ceil((float)Padding + (biggestGlyph + Padding) * AtlasGridSize);
			byte[] finalImage = new byte[imageSize * imageSize];

			// Calculate the Bitmap of each glyph and then blit it into #finalImage
			for (int i = 33; i < 127; i++) {
				int fx = (i - 33) % AtlasGridSize, fy = MathH.Floor((i - 33f) / AtlasGridSize);
				char c = (char)i;

				error = FT.FT_Load_Char(fontFacade.Face, c, FT.FT_LOAD_RENDER);
				if (error != FT_Error.FT_Err_Ok) { throw new($"Error loading font. Error code: {error}"); }

				ushort width = (ushort)fontFacade.GlyphMetricWidth;
				ushort height = (ushort)fontFacade.GlyphMetricHeight;

				unsafe {
					GlyphMap[c] = new(width, height, (short)((int)fontFacade.GlyphSlot->metrics.horiBearingX >> 6), (short)(height - ((int)fontFacade.FaceRec->glyph->metrics.horiBearingY >> 6)),
							(ushort)fontFacade.GlyphMetricHorizontalAdvance);
				}

				Glyph glyph = GlyphMap[c];
				int start = Padding + Padding * imageSize + fx * (biggestGlyph + Padding) + fy * (biggestGlyph + Padding) * imageSize;

				byte[] bitmap = new byte[width * height];
				Marshal.Copy(fontFacade.GlyphBitmap.buffer, bitmap, 0, bitmap.Length);

				for (int yy = 0; yy < glyph.Height; yy++) {
					for (int xx = 0; xx < glyph.Width; xx++) { finalImage[start + xx + yy * imageSize] = bitmap[xx + yy * glyph.Width]; }
				}

				float x = Padding + fx * (biggestGlyph + Padding);
				float y = Padding + fy * (biggestGlyph + Padding) + biggestGlyph - (biggestGlyph - glyph.Height);
				glyph.SetTexCoords(x / imageSize, y / imageSize, (x + glyph.Width) / imageSize, (y - glyph.Height) / imageSize);
			}

			// Calculate space size
			error = FT.FT_Load_Char(fontFacade.Face, ' ', FT.FT_LOAD_DEFAULT);
			if (error != FT_Error.FT_Err_Ok) { throw new($"Error loading font. Error code: {error}"); }
			spaceSize = (ushort)fontFacade.GlyphMetricHorizontalAdvance;

			return FontTexture = new SimpleTexture(finalImage, imageSize, imageSize, TextureMinFilter.Linear, TextureMagFilter.Linear) { PixelFormat = PixelFormat.Red, PixelInternalFormat = PixelInternalFormat.R8, };
		}

		public override List<Mesh> GetMesh(string text, float sizeOffset, float wordWrap = 0, float z = 0) {
			if (sizeOffset > Padding) {
				Logger.Warn($"SizeOffset cannot be above Padding ({Padding}). Was {sizeOffset}. Clamping to Padding...");
				sizeOffset = Padding;
			}

			List<Mesh> meshes = new();

			float x = 0;
			float y = 0; // TODO setup wordwrap

			float offset = sizeOffset / 2f;
			float uvoffset = offset / ((SimpleTexture)FontTexture).Width; // Only works because AtlasGridSize is hardcoded to be square

			void SetupGlyph(char c) {
				if (!GlyphMap.TryGetValue(c, out Glyph? glyph)) {
					Logger.Warn($"Tried to load unknown character. Id: {(ushort)c}");
					return;
				}

				meshes.Add(Quads.WH(x - offset + glyph.BearingX, y + glyph.BearingY + (biggestGlyph - glyph.Height) - offset, glyph.Width + offset * 2f, glyph.Height + offset * 2f, z, glyph.U0 - uvoffset, glyph.V0 + uvoffset,
						glyph.U1 + uvoffset, glyph.V1 - uvoffset));

				x += glyph.Advance;
			}

			if (wordWrap == 0) {
				foreach (char c in text) {
					if (c == ' ') {
						x += spaceSize;
						continue;
					}

					SetupGlyph(c);
				}

				return meshes;
			}

			foreach (string str in text.Split(' ')) {
				if (x + GetWidth(str) > wordWrap) {
					x = 0;
					y += FontSize * 1.5f;
				}

				foreach (char c in str) { SetupGlyph(c); }
				x += spaceSize;
			}

			return meshes;
		}

		public override float GetWidth(string text) {
			float curX = 0;

			foreach (char c in text) {
				if (c == ' ') {
					curX += spaceSize;
					continue;
				}

				curX += GlyphMap[c].Advance;
			}

			return curX;
		}
	}
}