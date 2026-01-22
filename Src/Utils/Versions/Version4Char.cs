using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Engine3.Utils.Versions {
	[PublicAPI]
	public readonly record struct Version4Char : IPackableVersion {
		private const byte HotfixCharOffset = (byte)'a' - 1;

		public required uint Packed { get; init; }

		public byte Release { get => (byte)(Packed >> 24); init => Packed |= (uint)(value << 24); }
		public byte Major { get => (byte)(Packed >> 16); init => Packed |= (uint)(value << 16); }
		public byte Minor { get => (byte)(Packed >> 8); init => Packed |= (uint)(value << 8); }

		public byte HotfixByte => (byte)Packed;
		public char Hotfix {
			get {
				byte byteValue = HotfixByte;
				return (char)(byteValue == char.MinValue ? byteValue : byteValue + HotfixCharOffset);
			}
			init {
				byte byteValue = value == char.MinValue ? (byte)value : (byte)(value - HotfixCharOffset);
				Packed |= byteValue;
			}
		}

		[SetsRequiredMembers] public Version4Char(uint packed) => Packed = packed;

		[SetsRequiredMembers]
		public Version4Char(byte release, byte major, byte minor, char hotfix = char.MinValue) {
			Release = release;
			Major = major;
			Minor = minor;
			Hotfix = hotfix is >= 'a' and <= 'z' or char.MinValue ? hotfix : throw new ArgumentOutOfRangeException(nameof(Hotfix), $"{nameof(Hotfix)} is limited to [a-z]");
		}

		public override int GetHashCode() => (int)Packed;
		public override string ToString() => $"{Release}.{Major}.{Minor}{(Hotfix == 0 ? string.Empty : Hotfix)}";
	}

	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct Version4Char<T> where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T> {
		public required T Release { get; init; }
		public required T Major { get; init; }
		public required T Minor { get; init; }

		public char Hotfix {
			get;
			init => field = value is >= 'a' and <= 'z' or char.MinValue ? value : throw new ArgumentOutOfRangeException(nameof(Hotfix), $"{nameof(Hotfix)} is limited to [a-z]. if you're at z, you should probably rethink things");
		}

		[SetsRequiredMembers]
		public Version4Char(T release, T major, T minor, char hotfix = char.MinValue) {
			Release = release;
			Major = major;
			Minor = minor;
			Hotfix = hotfix;
		}

		public override int GetHashCode() => HashCode.Combine(Release, Major, Minor, Hotfix);
		public override string ToString() => $"{Release}.{Major}.{Minor}{(Hotfix == 0 ? string.Empty : Hotfix)}";
	}
}