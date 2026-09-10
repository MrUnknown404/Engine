using JetBrains.Annotations;

namespace Engine4.Utility.Versions;

[PublicAPI]
public readonly record struct DateVersion : IPackableVersion { // TODO copy this to the other versions?
	private const byte HotfixCharOffset = (byte)'a' - 1;

	public const uint YearMask = 0b_11111111_11110000_00000000_00000000;
	public const uint WeekMask = 0b_00000000_00001111_11000000_00000000;
	public const uint MinorMask = 0b_00000000_00000000_00111111_11000000;
	public const uint HotfixMask = 0b_00000000_00000000_00000000_00111111;

	public const byte YearBitCount = 12;
	public const byte WeekBitCount = 6;
	public const byte MinorBitCount = 8;
	public const byte HotfixBitCount = 6;

	public const byte YearOffset = HotfixBitCount + MinorBitCount + WeekBitCount;
	public const byte WeekOffset = HotfixBitCount + MinorBitCount;
	public const byte MinorOffset = HotfixBitCount;
	public const byte HotfixOffset = 0;

	// 2 ^ bit - 1
	public const ushort YearMaxValue = (1 << YearBitCount) - 1;
	public const ushort WeekMaxValue = (1 << WeekBitCount) - 1;
	public const ushort MinorMaxValue = (1 << MinorBitCount) - 1;
	public const ushort HotfixMaxValue = (1 << HotfixBitCount) - 1;

	public uint Packed { get; init; }

	public ushort Year { get => (ushort)((Packed & YearMask) >> YearOffset); init => Packed |= (uint)(value << YearOffset) & YearMask; }
	public byte Week { get => (byte)((Packed & WeekMask) >> WeekOffset); init => Packed |= (uint)(value << WeekOffset) & WeekMask; }
	public byte Minor { get => (byte)((Packed & MinorMask) >> MinorOffset); init => Packed |= (uint)(value << MinorOffset) & MinorMask; }
	public byte HotfixByte { get => (byte)((Packed & HotfixMask) >> HotfixOffset); init => Packed |= (uint)(value << HotfixOffset) & HotfixMask; }

	public char Hotfix {
		get {
			byte byteValue = HotfixByte;
			return (char)(byteValue == char.MinValue ? byteValue : byteValue + HotfixCharOffset);
		}
		init {
			if (value is < 'a' or > 'z') { throw new ArgumentOutOfRangeException(nameof(Hotfix), $"{nameof(Hotfix)} is limited to [a-z]"); }
			byte byteValue = value == char.MinValue ? (byte)value : (byte)(value - HotfixCharOffset);
			Packed |= byteValue;
		}
	}

	public DateVersion(uint packed) => Packed = packed;

	public DateVersion(ushort year, byte week, byte minor, char hotfix = char.MinValue) {
		Year = year;
		Week = week;
		Minor = minor;
		Hotfix = hotfix;
	}

	public override int GetHashCode() => (int)Packed;
	public override string ToString() => $"{Year}.{Week}.{Minor}{(Hotfix == char.MinValue ? string.Empty : Hotfix)}";
}