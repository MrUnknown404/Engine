using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.GL.OldMesh {
	[Obsolete("Going to be removed.")]
	[PublicAPI]
	public class PureMeshData : IMeshData {
		private float[] vertices = Array.Empty<float>();
		private uint[] indices = Array.Empty<uint>();

		public void Set(float[] vertices, uint[] indices) {
			this.vertices = vertices;
			this.indices = indices;
		}

		public void Reset() {
			vertices = Array.Empty<float>();
			indices = Array.Empty<uint>();
		}

		public float[] GetVertices() => vertices;
		public uint[] GetIndices() => indices;
	}
}