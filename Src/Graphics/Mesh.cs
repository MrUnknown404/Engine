namespace Engine3.Graphics {
	public class Mesh { // TODO impl
		public byte[] Data { get; } // How should i store this? because we can split our data into multiple buffers,
		// this could take in a layout class and then a method will convert the mesh data into the layout

		public Mesh(byte[] data) => Data = data;
	}
}