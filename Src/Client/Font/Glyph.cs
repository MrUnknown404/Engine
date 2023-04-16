namespace USharpLibs.Engine.Client.Font {
	internal sealed class Glyph {
		internal float W { get; }
		internal float H { get; }
		internal float X0 { get; }
		internal float Y0 { get; }
		internal float X1 { get; }
		internal float Y1 { get; }
		internal float X2 { get; }
		internal float Y2 { get; }
		internal float X3 { get; }
		internal float Y3 { get; }

		internal Glyph(float x, float y, float w, float h) {
			W = w;
			H = h;

			X0 = x;
			Y0 = y;
			X1 = x + w;
			Y1 = y;
			X2 = x;
			Y2 = y + h;
			X3 = x + w;
			Y3 = y + h;
		}
	}
}