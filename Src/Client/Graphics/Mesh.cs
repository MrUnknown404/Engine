namespace Engine3.Client.Graphics {
	public class Mesh {
		public byte[] Vertices { get; }
		public uint[] Indices { get; }

		public Mesh(byte[] vertices, uint[] indices) {
			Vertices = vertices;
			Indices = indices;
		}
	}
}