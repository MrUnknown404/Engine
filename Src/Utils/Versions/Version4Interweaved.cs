using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Engine3.Utils.Versions {
	/// <summary>
	/// <see cref="Packed"/> depacks into the following values:
	/// <code>
	/// Release == Packed[28..31]
	/// Major   == Packed[20..27]
	/// Minor   == Packed[5..19]
	/// Hotfix  == Packed[0..4]
	/// </code>
	/// </summary>
	[PublicAPI]
	public readonly record struct Version4Interweaved : IPackableVersion {
		private const byte ReleaseBitMask = 0b00001111;
		private const byte ReleaseOffset = MajorOffset + 8;

		private const byte MajorBitMask = 0b11111111;
		private const byte MajorOffset = MinorOffset + 15;

		private const ushort MinorBitMask = 0b01111111_11111111;
		private const byte MinorOffset = HotfixOffset + 5;

		private const byte HotfixBitMask = 0b00011111;
		private const byte HotfixOffset = 0;
		private const byte HotfixCharOffset = (byte)'a' - 1;

		public required uint Packed { get; init; }

		/// <summary>
		/// Release value depacked from <see cref="Packed"/>
		/// <code>
		/// Bits   == Packed[28..31]
		/// Mask   == 0b00001111
		/// Offset == 28
		/// </code>
		/// </summary>
		public byte Release {
			get => (byte)((Packed >> ReleaseOffset) & ReleaseBitMask);
			init {
				if (value > ReleaseBitMask) { throw new ArgumentException($"{nameof(Release)} cannot be above {ReleaseBitMask}"); }
				Packed |= (uint)(value << ReleaseOffset);
			}
		}

		/// <summary>
		/// Major value depacked from <see cref="Packed"/>
		/// <code>
		/// Bits   == Packed[20..27]
		/// Mask   == 0b11111111
		/// Offset == 20
		/// </code>
		/// </summary>
		public byte Major { get => (byte)((Packed >> MajorOffset) & MajorBitMask); init => Packed |= (uint)(value << MajorOffset); }

		/// <summary>
		/// Minor value depacked from <see cref="Packed"/>
		/// <code>
		/// Bits   == Packed[5..19]
		/// Mask   == 0b01111111_11111111
		/// Offset == 5
		/// </code>
		/// </summary>
		public ushort Minor {
			get => (ushort)((Packed >> MinorOffset) & MinorBitMask);
			init {
				if (value > MinorBitMask) { throw new ArgumentException($"{nameof(Minor)} cannot be above {MinorBitMask}"); }
				Packed |= (uint)(value << MinorOffset);
			}
		}

		/// <summary>
		/// Hotfix value depacked from <see cref="Packed"/>
		/// <code>
		/// Bits   == Packed[0..4]
		/// Mask   == 0b00011111
		/// Offset == 0
		/// </code>
		/// </summary>
		public byte HotfixByte => (byte)((Packed >> HotfixOffset) & HotfixBitMask);

		/// <inheritdoc cref="HotfixByte" />
		public char Hotfix {
			get {
				byte byteValue = HotfixByte;
				return (char)(byteValue == char.MinValue ? byteValue : byteValue + HotfixCharOffset);
			}
			init {
				byte byteValue = value == char.MinValue ? (byte)value : (byte)(value - HotfixCharOffset);
				if (byteValue > HotfixBitMask) { throw new ArgumentException($"{nameof(Hotfix)} cannot be above {HotfixBitMask}"); }
				Packed |= (uint)(byteValue << HotfixOffset);
			}
		}

		[SetsRequiredMembers] public Version4Interweaved(uint packed) => Packed = packed;

		[SetsRequiredMembers]
		public Version4Interweaved(byte release, byte major, ushort minor, char hotfix = char.MinValue) {
			Release = release;
			Major = major;
			Minor = minor;
			Hotfix = hotfix is >= 'a' and <= 'z' or char.MinValue ? hotfix : throw new ArgumentOutOfRangeException(nameof(Hotfix), $"{nameof(Hotfix)} is limited to [a-z]");
		}

		public override int GetHashCode() => (int)Packed;
		public override string ToString() => $"{Release}.{Major}.{Minor}{(Hotfix == 0 ? string.Empty : Hotfix)}";
	}
}