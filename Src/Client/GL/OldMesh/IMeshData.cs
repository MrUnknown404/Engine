using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.GL.OldMesh {
	[Obsolete("Going to be removed.")]
	[PublicAPI]
	public interface IMeshData {
		public float[] GetVertices();
		public uint[] GetIndices();
		public void Reset();
	}
}