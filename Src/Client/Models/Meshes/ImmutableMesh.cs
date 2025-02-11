using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models.Meshes {
	public abstract class ImmutableMesh : Mesh {
		private uint[] Indices { get; }

		protected internal override int IndexCount => Indices.Length;

		protected ImmutableMesh(uint[] indices) {
			_ = MeshErrorHandler.Assert(indices.Length % 3 != 0, static () => new(MeshErrorHandler.Reason.IncorrectlySizedIndexArray));

			Indices = indices;
		}

		protected override void BindIndices(uint ebo, BufferUsageHint bufferHint) {
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint), Indices, bufferHint);
		}
	}
}