using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine3.Utils {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct Version3 {
		public required byte Release { get; init; }
		public required byte Major { get; init; }
		public required byte Minor { get; init; }

		public uint Packed => (uint)((Release << 16) | (Major << 8) | Minor);

		[SetsRequiredMembers]
		public Version3(byte release, byte major, byte minor) {
			Release = release;
			Major = major;
			Minor = minor;
		}

		[SetsRequiredMembers]
		public Version3(uint packed) {
			Release = (byte)(packed >> 16);
			Major = (byte)(packed >> 8);
			Minor = (byte)packed;
		}

		public override int GetHashCode() => (int)Packed;
		public override string ToString() => $"{Release}.{Major}.{Minor}";
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct Version3<T> where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T> {
		public required T Release { get; init; }
		public required T Major { get; init; }
		public required T Minor { get; init; }

		[SetsRequiredMembers]
		public Version3(T release, T major, T minor) {
			Release = release;
			Major = major;
			Minor = minor;
		}

		public override int GetHashCode() => HashCode.Combine(Release, Major, Minor);
		public override string ToString() => $"{Release}.{Major}.{Minor}";
	}
}