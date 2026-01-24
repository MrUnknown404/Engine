using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Engine3.Utility.Versions {
	[PublicAPI]
	public readonly record struct Version4 : IPackableVersion {
		public required uint Packed { get; init; }

		public byte Release { get => (byte)(Packed >> 24); init => Packed |= (uint)(value << 24); }
		public byte Major { get => (byte)(Packed >> 16); init => Packed |= (uint)(value << 16); }
		public byte Minor { get => (byte)(Packed >> 8); init => Packed |= (uint)(value << 8); }
		public byte Hotfix { get => (byte)Packed; init => Packed |= value; }

		[SetsRequiredMembers] public Version4(uint packed) => Packed = packed;

		[SetsRequiredMembers]
		public Version4(byte release, byte major, byte minor, byte hotfix = byte.MinValue) {
			Release = release;
			Major = major;
			Minor = minor;
			Hotfix = hotfix;
		}

		public override int GetHashCode() => (int)Packed;
		public override string ToString() => $"{Release}.{Major}.{Minor}.{Hotfix}";
	}

	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct Version4<T> where T : unmanaged, IBinaryInteger<T>, IUnsignedNumber<T> {
		public required T Release { get; init; }
		public required T Major { get; init; }
		public required T Minor { get; init; }
		public T Hotfix { get; init; }

		[SetsRequiredMembers]
		public Version4(T release, T major, T minor) {
			Release = release;
			Major = major;
			Minor = minor;
		}

		public override int GetHashCode() => HashCode.Combine(Release, Major, Minor, Hotfix);
		public override string ToString() => $"{Release}.{Major}.{Minor}.{Hotfix}";
	}
}