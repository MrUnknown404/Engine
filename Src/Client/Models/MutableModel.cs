using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models {
	public sealed class MutableModel<TMesh> : Model where TMesh : Mesh<List<uint>> {
		private List<MeshBufferData> MeshBufferData { get; } = new();
		private List<TMesh> Meshes { get; } = new();

		protected internal override bool IsBuildMeshEmpty => Meshes.Count == 0;
		protected internal override bool IsDrawDataEmpty => MeshBufferData.Count == 0;

		private bool isDirty;

		[SetsRequiredMembers] public MutableModel(BufferUsageHint bufferHint = BufferUsageHint.DynamicDraw) => BufferHint = bufferHint;

		protected internal override void Draw() {
			foreach (MeshBufferData data in MeshBufferData) {
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, data.EBO);
				GL.DrawElements(PrimitiveType.Triangles, data.Count, DrawElementsType.UnsignedInt, 0);
			}
		}

		protected internal override void Build() {
			isDirty = false;

			foreach (MeshBufferData data in MeshBufferData) { // Reuse buffers instead
				GL.DeleteBuffer(data.VBO);
				GL.DeleteBuffer(data.EBO);
			}

			MeshBufferData.Clear();

			foreach (TMesh mesh in Meshes) {
				uint vbo = (uint)GL.GenBuffer();
				uint ebo = (uint)GL.GenBuffer();

				MeshBufferData.Add(new() { VBO = vbo, EBO = ebo, Count = mesh.IndexCount, });
				mesh.BindToBuffer(vbo, ebo, BufferHint);
			}
		}

		protected internal override bool CanBuild() => isDirty;

		// TODO mutation methods
	}
}