namespace Engine3.Utility.Versions {
	public interface IPackableVersion {
		/// <summary>Packed representation of a version implementation</summary>
		public uint Packed { get; }
	}
}