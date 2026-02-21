namespace Engine3.Client.Graphics {
	public class Model {
		public RenderData[] RenderDataList { get; }
		private readonly RenderMesh[] meshes;

		public Model(RenderMesh[] meshes, byte vertexStride) {
			this.meshes = meshes;
			RenderDataList = MakeRenderData(meshes, vertexStride);
		}

		public void Collect(out byte[] vertices, out uint[] indices) { // TODO cache
			List<byte> vertexList = new();
			List<uint> indexList = new();

			foreach (RenderMesh mesh in meshes) {
				vertexList.AddRange(mesh.Vertices);
				indexList.AddRange(mesh.Indices);
			}

			vertices = vertexList.ToArray();
			indices = indexList.ToArray();
		}

		private static RenderData[] MakeRenderData(RenderMesh[] meshes, byte vertexStride) {
			List<RenderData> renderData = new();

			int vertexOffset = 0;

			foreach (RenderMesh mesh in meshes) {
				uint indicesLength = (uint)mesh.Indices.Length;

				renderData.Add(new(indicesLength, vertexOffset) { Material = mesh.Material, });
				vertexOffset += mesh.Vertices.Length / vertexStride;
			}

			return renderData.ToArray();
		}

		public class RenderData {
			public uint IndexCount { get; }
			public int VertexOffset { get; }
			public Material? Material { get; init; }

			internal RenderData(uint indexCount, int vertexOffset) {
				IndexCount = indexCount;
				VertexOffset = vertexOffset;
			}
		}
	}
}