namespace USharpLibs.Engine2.Client.Models.Vertex {
	public interface IVertex {
		public static abstract byte SizeInBytes { get; }
		public void Collect(ref byte[] arr, int index);
	}
}