namespace USharpLibs.Engine2.Client.Models {
	public interface IVertex {
		public static abstract byte SizeInBytes { get; }
		public void Collect(ref byte[] arr, int index);
	}
}