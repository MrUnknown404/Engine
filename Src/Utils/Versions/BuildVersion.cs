using System.Diagnostics.CodeAnalysis;

namespace Engine3.Utils.Versions {
	public readonly record struct BuildVersion : IPackableVersion {
		public required uint Version { get; init; }
		public uint Packed => Version;
		[SetsRequiredMembers] public BuildVersion(uint version) => Version = version;
	}
}