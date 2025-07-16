namespace Engine3.Client.Model.Mesh.Vertex {
	public readonly record struct VertexLayout {
		public VertexAttribute[] Layout { get; }
		public byte Size { get; }

		public VertexLayout(VertexAttribute[] layout) {
			Layout = layout;
			Size = (byte)layout.Sum(static v => v.ElementCount * v.ElementByteSize);
		}
	}
}