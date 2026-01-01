using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine3.Utils {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct Version4 {
		public required byte Release { get; init; }
		public required byte Major { get; init; }
		public required byte Minor { get; init; }
		private readonly byte hotfix; // this is a byte instead of a char to reduce the struct size to 4 bytes

		public char Hotfix { get => (char)hotfix; init => hotfix = value is >= 'a' and <= 'z' ? (byte)value : throw new ArgumentOutOfRangeException(nameof(Hotfix), $"{nameof(Hotfix)} is limited to [a-z]"); }

		public uint Packed => (uint)((Release << 24) | (Major << 16) | (Minor << 8) | hotfix);

		[SetsRequiredMembers]
		public Version4(byte release, byte major, byte minor, char hotfix = char.MinValue) {
			Release = release;
			Major = major;
			Minor = minor;
			this.hotfix = (byte)hotfix;
		}

		[SetsRequiredMembers]
		public Version4(uint packed) {
			Release = (byte)(packed >> 24);
			Major = (byte)(packed >> 16);
			Minor = (byte)(packed >> 8);
			hotfix = (byte)packed;
		}

		public override int GetHashCode() => (int)Packed;
		public override string ToString() => $"{Release}.{Major}.{Minor}{(Hotfix == 0 ? string.Empty : Hotfix)}";
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct Version4<T> where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T> {
		public required T Release { get; init; }
		public required T Major { get; init; }
		public required T Minor { get; init; }

		public char Hotfix {
			get;
			init => field = value is >= 'a' and <= 'z' ? value : throw new ArgumentOutOfRangeException(nameof(Hotfix), $"{nameof(Hotfix)} is limited to [a-z]. if you're at z, you should probably rethink things");
		}

		[SetsRequiredMembers]
		public Version4(T release, T major, T minor, char hotfix = char.MinValue) {
			Release = release;
			Major = major;
			Minor = minor;
			if (hotfix != char.MinValue) { Hotfix = hotfix; } // check so we don't throw?
		}

		public override int GetHashCode() => HashCode.Combine(Release, Major, Minor, Hotfix);
		public override string ToString() => $"{Release}.{Major}.{Minor}{(Hotfix == 0 ? string.Empty : Hotfix)}";
	}
}