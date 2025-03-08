using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace USharpLibs.Engine2.Utils {
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	public readonly record struct Version4 {
		public required byte Release { get; init; }
		public required byte Major { get; init; }
		public required byte Minor { get; init; }
		public char Hotfix {
			get;
			init => field = value is >= 'a' and <= 'z' ? value : throw new ArgumentOutOfRangeException(nameof(Hotfix), $"{nameof(Hotfix)} is limited to [a-z]. if you're at z, you should probably increment minor");
		}

		[SetsRequiredMembers]
		public Version4(byte release, byte major, byte minor, char hotfix = default) {
			Release = release;
			Major = major;
			Minor = minor;
			if (hotfix != default) { Hotfix = hotfix; }
		}

		public override int GetHashCode() => HashCode.Combine(Release, Major, Minor, Hotfix);
		public override string ToString() => $"{Release}.{Major}.{Minor}{(Hotfix == 0 ? string.Empty : Hotfix)}";
	}
}