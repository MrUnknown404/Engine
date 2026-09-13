namespace Engine4.Utility.Versions;

public readonly record struct BuildVersion : IPackableVersion {
	public uint Version { get; init; }
	public uint Packed => Version;

	public BuildVersion(uint version) => Version = version;

	public override int GetHashCode() => (int)Packed;
	public override string ToString() => $"{Packed}";
}