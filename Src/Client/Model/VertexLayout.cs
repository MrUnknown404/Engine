using Engine3.Client.Model.Mesh.Vertex;

namespace Engine3.Client.Model {
	public sealed class VertexLayout {
		public VertexAttribute[] Layout { get; }
		public byte Size { get; }

		public VertexLayout(VertexAttribute[] layout) {
			Layout = layout;
			Size = (byte)layout.Sum(static v => v.ElementCount * v.ElementByteSize);
		}
	}
}