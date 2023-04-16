using JetBrains.Annotations;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Client.GL.OldMesh {
	[Obsolete("Going to be removed.")]
	[PublicAPI]
	public class AutoIndexMeshData : IMeshData {
		private readonly List<Shape> datas = new();

		private float[] verticesCache = Array.Empty<float>();
		private uint[] indicesCache = Array.Empty<uint>();
		private bool requiresCacheReset = true;

		public void Add(Shape data) {
			datas.Add(data);
			requiresCacheReset = true;
		}

		public void Reset() {
			datas.Clear();
			requiresCacheReset = true;
		}

		private void RebuildCache() {
			requiresCacheReset = false;

			List<uint> indexList = new();
			Dictionary<ValueTuple<float, float, float, float, float>, uint> indexMap = new();
			List<ValueTuple<float, float, float, float, float>> vertexList = new();

			foreach (Shape data in datas) {
				for (int i = 0; i < data.Vertices.Length / 5; i++) {
					ValueTuple<float, float, float, float, float> d = new(data.Vertices[i * 5], data.Vertices[i * 5 + 1], data.Vertices[i * 5 + 2],
						data.Vertices[i * 5 + 3], data.Vertices[i * 5 + 4]);

					vertexList.Add(d);
					indexList.Add(indexMap.ComputeIfAbsent(d, _ => (uint)indexMap.Count));
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