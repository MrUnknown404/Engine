using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models.Separated {
	public class ImmSeparateModel<TVertex> : ModelBase<MeshBufferData[], ImmSeparateMesh<TVertex>[], ImmSeparateMesh<TVertex>> where TVertex : struct, IVertexAttribute {
		private bool WasBuilt { get; set; }
		protected internal override bool IsBuildMeshEmpty => Meshes.Length == 0;
		protected internal override bool IsDrawDataEmpty => !WasBuilt;

		[SetsRequiredMembers] public ImmSeparateModel(ImmSeparateMesh<TVertex>[] meshes) : base(new MeshBufferData[meshes.Length], meshes) => BufferHint = BufferUsageHint.StaticDraw;

		protected internal override void Build() {
			for (int i = 0; i < Meshes.Length; i++) {
				ImmSeparateMesh<TVertex> mesh = Meshes[i];
				uint vbo = (uint)GL.GenBuffer();
				uint ebo = (uint)GL.GenBuffer();

				MeshBufferData[i] = new() { VBO = vbo, EBO = ebo, Count = mesh.Indices.Length, };
				mesh.BindToBuffer(vbo, ebo, BufferHint);
			}

			WasBuilt = true;
		}

		protected internal override bool CanBuild() => !ModelErrorHandler.Assert(WasBuilt, static () => new(ModelErrorHandler.Reason.ImmutableModelAlreadyBuilt));
	}
}