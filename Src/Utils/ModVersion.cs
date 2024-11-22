using System.Runtime.InteropServices;

namespace USharpLibs.Engine2.Utils {
	[PublicAPI]
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	public readonly record struct ModVersion {
		public required byte Release { get; init; }
		public required byte Major { get; init; }
		public required byte Minor { get; init; }
		public char Hotfix { get; init => field = value is >= 'a' and <= 'z' ? value : throw new ArgumentOutOfRangeException(nameof(Hotfix), "Hotfix is limited to [a-z]"); }

		public override string ToString() => $"{Release}, {Major}, {Minor}{(Hotfix == 0 ? string.Empty : Hotfix)}";
	}
}