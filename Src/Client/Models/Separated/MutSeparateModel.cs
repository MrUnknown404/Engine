using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;

namespace USharpLibs.Engine2.Client.Models.Separated {
	public class MutSeparateModel<TMesh> : ModelBase<List<MeshBufferData>, List<TMesh>, TMesh> where TMesh : ISeparateMesh {
		public bool IsDirty { get; private set; }
		protected internal override bool IsBuildMeshEmpty => Meshes.Count == 0;
		protected internal override bool IsDrawDataEmpty => MeshBufferData.Count == 0;

		[SetsRequiredMembers] public MutSeparateModel() : base(new(), new()) => BufferHint = BufferUsageHint.DynamicDraw;

		protected internal override void Build() {
			foreach (MeshBufferData data in MeshBufferData) { // TODO Reuse buffers when possible
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

		protected internal override bool CanBuild() => IsDirty;

		// TODO mutation methods
	}
}