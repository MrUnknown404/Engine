using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models.Meshes {
	public abstract class MutableMesh : Mesh {
		private List<uint> Indices { get; } = new();

		protected internal override int IndexCount => Indices.Count;

		protected override void BindIndices(uint ebo, BufferUsageHint bufferHint) {
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Count * sizeof(uint), Indices.ToArray(), bufferHint);
		}

		// TODO mutation methods & check!
	}
}