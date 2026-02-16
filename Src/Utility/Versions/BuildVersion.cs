namespace Engine3.Utility.Versions {
	public readonly record struct BuildVersion : IPackableVersion {
		public uint Version { get; init; }
		public uint Packed => Version;

		public BuildVersion(uint version) => Version = version;
	}
}