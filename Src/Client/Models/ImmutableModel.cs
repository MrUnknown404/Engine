using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine2.Debug;

namespace USharpLibs.Engine2.Client.Models {
	public sealed class ImmutableModel<TMesh> : Model where TMesh : InterleavedMesh {
		private MeshBufferData[] MeshBufferData { get; }
		private TMesh[] Meshes { get; }

		protected internal override bool IsBuildMeshEmpty => Meshes.Length == 0;
		protected internal override bool IsDrawDataEmpty => !wasBuilt;

		private bool wasBuilt;

		[SetsRequiredMembers]
		public ImmutableModel(TMesh[] meshes) {
			MeshBufferData = new MeshBufferData[meshes.Length];
			Meshes = meshes;
			BufferHint = BufferUsageHint.StaticDraw;
		}

		protected internal override void Draw() {
			foreach (MeshBufferData data in MeshBufferData) {
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, data.EBO);
				GL.DrawElements(PrimitiveType.Triangles, data.Count, DrawElementsType.UnsignedInt, 0);
			}
		}

		protected internal override void Build() {
			wasBuilt = true;

			for (int i = 0; i < Meshes.Length; i++) {
				TMesh mesh = Meshes[i];
				uint vbo = (uint)GL.GenBuffer();
				uint ebo = (uint)GL.GenBuffer();

				MeshBufferData[i] = new() { VBO = vbo, EBO = ebo, Count = mesh.IndexCount, };
				mesh.BindToBuffer(vbo, ebo, BufferHint);
			}
		}

		protected internal override bool CanBuild() => !ModelErrorHandler.Assert(wasBuilt, static () => new(ModelErrorHandler.Reason.ImmutableModelAlreadyBuilt));
	}
}