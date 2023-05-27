namespace USharpLibs.Engine.Client.Fonts {
	public class Glyph {
		public ushort Width { get; }
		public ushort Height { get; }
		public short BearingX { get; }
		public short BearingY { get; }
		public ushort Advance { get; }

		public float U0 { get; private set; }
		public float V0 { get; private set; }
		public float U1 { get; private set; }
		public float V1 { get; private set; }

		public Glyph(ushort width, ushort height, short bearingX, short bearingY, ushort advance) {
			Width = width;
			Height = height;
			BearingX = bearingX;
			BearingY = bearingY;
			Advance = advance;
		}

		public void SetTexCoords(float u0, float v0, float u1, float v1) {
			U0 = u0;
			V0 = v0;
			U1 = u1;
			V1 = v1;
		}
	}
}