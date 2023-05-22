using JetBrains.Annotations;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Utils {
	[PublicAPI]
	public readonly struct HexPos : IEquatable<HexPos> {
		public static HexPos Identity { get; } = new();

		public static HexPos East { get; } = new(1, 0, -1);
		public static HexPos NorthEast { get; } = new(1, -1, 0);
		public static HexPos NorthWest { get; } = new(0, -1, 1);
		public static HexPos West { get; } = new(-1, 0, 1);
		public static HexPos SouthWest { get; } = new(-1, 1, 0);
		public static HexPos SouthEast { get; } = new(0, 1, -1);

		public static HexPos[] Directions { get; } = { East, NorthEast, NorthWest, West, SouthWest, SouthEast, };

		public int Q { get; } = 0;
		public int R { get; } = 0;
		public int S { get; } = 0;

		public HexPos(int q, int r, int s) {
			if (q + r + s != 0) {
				Logger.Warn($"Attempted to create HexPos with invalid coordinates: ({q}, {r}, {s}). Hexagon coordinates when added together must equal 0.");
				return;
			}

			Q = q;
			R = r;
			S = s;
		}

		public HexPos() { }

		public static int Distance(in HexPos start, in HexPos end) => (Math.Abs(start.Q - end.Q) + Math.Abs(start.R - end.R) + Math.Abs(start.S - end.S)) / 2;
		public int Distance(in HexPos pos) => (Math.Abs(Q - pos.Q) + Math.Abs(R - pos.R) + Math.Abs(S - pos.S)) / 2;

		public bool Equals(HexPos other) => Q == other.Q && R == other.R && S == other.S;

		public static bool operator ==(HexPos hex, HexPos other) => hex.Equals(other);
		public static bool operator !=(HexPos hex, HexPos other) => !hex.Equals(other);

		public static HexPos operator +(HexPos hex, HexPos value) => new(hex.Q + value.Q, hex.R + value.R, hex.S + value.S);
		public static HexPos operator +(HexPos hex, int value) => new(hex.Q + value, hex.R + value, hex.S + value);

		public static HexPos operator -(HexPos hex, HexPos value) => new(hex.Q - value.Q, hex.R - value.R, hex.S - value.S);
		public static HexPos operator -(HexPos hex, int value) => new(hex.Q - value, hex.R - value, hex.S - value);

		public static HexPos operator *(HexPos hex, HexPos value) => new(hex.Q * value.Q, hex.R * value.R, hex.S * value.S);
		public static HexPos operator *(HexPos hex, int value) => new(hex.Q * value, hex.R * value, hex.S * value);

		public static HexPos operator /(HexPos hex, HexPos value) => new(hex.Q / value.Q, hex.R / value.R, hex.S / value.S);
		public static HexPos operator /(HexPos hex, int value) => new(hex.Q / value, hex.R / value, hex.S / value);

		public override bool Equals(object? obj) => obj is HexPos other && Equals(other);
		public override int GetHashCode() => (Q, R, S).GetHashCode();

		public override string ToString() => $"({Q}, {R}, {S})";
	}
}