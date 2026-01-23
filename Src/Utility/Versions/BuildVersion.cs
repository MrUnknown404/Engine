using System.Diagnostics.CodeAnalysis;
using Engine3.Api;

namespace Engine3.Utility.Versions {
	public readonly record struct BuildVersion : IPackableVersion {
		public required uint Version { get; init; }
		public uint Packed => Version;
		[SetsRequiredMembers] public BuildVersion(uint version) => Version = version;
	}
}