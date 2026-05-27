namespace Engine3.Core.Utility.Versions;

public interface IPackableVersion {
	/// <summary> Packed representation of a version implementation </summary>
	public uint Packed { get; }
}