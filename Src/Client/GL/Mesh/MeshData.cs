using USharpLibs.Common.Utils.Extensions;

namespace USharpLibs.Engine.Client.GL.Mesh {
	public class MeshData {
		private readonly List<Shape> datas = new();

		private float[] verticesCache = Array.Empty<float>();
		private uint[] indicesCache = Array.Empty<uint>();
		private bool requiresCacheReset = true;

		public void Add(Shape data) {
			datas.Add(data);
			requiresCacheReset = true;
		}

		public virtual void Reset() {
			datas.Clear();
			requiresCacheReset = true;
		}

		private void RebuildCache() {
			requiresCacheReset = false;

			// Vertices
			List<ValueTuple<float, float, float, float, float>> vertexList = new();
			foreach (Shape data in datas) {
				for (int i = 0; i < data.Vertices.Length / 5; i++) {
					vertexList.Add(new(data.Vertices[i * 5], data.Vertices[i * 5 + 1], data.Vertices[i * 5 + 2], data.Vertices[i * 5 + 3], data.Vertices[i * 5 + 4]));
				}
			}

			vertexList = vertexList.Distinct().ToList();
			verticesCache = new float[vertexList.Count * 5];

			for (int i = 0; i < vertexList.Count; i++) {
				ValueTuple<float, float, float, float, float> vertex = vertexList[i];
				verticesCache[i * 5] = vertex.Item1;
				verticesCache[i * 5 + 1] = vertex.Item2;
				verticesCache[i * 5 + 2] = vertex.Item3;
				verticesCache[i * 5 + 3] = vertex.Item4;
				verticesCache[i * 5 + 4] = vertex.Item5;
			}

			// Indices
			List<uint> indexList = new();
			Dictionary<ValueTuple<float, float, float, float, float>, uint> indexMap = new();
			foreach (Shape data in datas) {
				for (int i = 0; i < data.Vertices.Length / 5; i++) {
					indexList.Add(indexMap.ComputeIfAbsent(new(data.Vertices[i * 5], data.Vertices[i * 5 + 1], data.Vertices[i * 5 + 2], data.Vertices[i * 5 + 3], data.Vertices[i * 5 + 4]), key => (uint)indexMap.Count));
				}
			}

			indicesCache = indexList.ToArray();
		}

		public float[] GetVertices() {
			if (requiresCacheReset) { RebuildCache(); }
			return verticesCache;
		}

		public uint[] GetIndices() {
			if (requiresCacheReset) { RebuildCache(); }
			return indicesCache;
		}
	}
}