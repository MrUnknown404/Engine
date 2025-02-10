using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models.Interleaved {
	[PublicAPI]
	public class ImmInterleavedModel<TVertex> : ModelBase<MeshBufferData[], ImmInterleavedMesh<TVertex>[], ImmInterleavedMesh<TVertex>> where TVertex : struct, IInterleavedVertex {
		protected internal override bool IsBuildMeshEmpty => Meshes.Length == 0;
		protected internal override bool IsDrawDataEmpty => !wasBuilt;

		private bool wasBuilt;

		[SetsRequiredMembers] public ImmInterleavedModel(ImmInterleavedMesh<TVertex>[] meshes) : base(new MeshBufferData[meshes.Length], meshes) => BufferHint = BufferUsageHint.StaticDraw;

		protected internal override void Build() {
			wasBuilt = true;

			for (int i = 0; i < Meshes.Length; i++) {
				ImmInterleavedMesh<TVertex> mesh = Meshes[i];
				uint vbo = (uint)GL.GenBuffer();
				uint ebo = (uint)GL.GenBuffer();

				MeshBufferData[i] = new() { VBO = vbo, EBO = ebo, Count = mesh.IndexCount, };
				mesh.BindToBuffer(vbo, ebo, BufferHint);
			}
		}

		protected internal override bool CanBuild() => !ModelErrorHandler.Assert(wasBuilt, static () => new(ModelErrorHandler.Reason.ImmutableModelAlreadyBuilt));
	}
}