using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;

namespace USharpLibs.Engine2.Client.Models.Interleaved {
	[PublicAPI]
	public class MutInterleavedModel<TMesh> : ModelBase<List<MeshBufferData>, List<TMesh>, TMesh> where TMesh : IInterleavedMesh, IMesh {
		protected internal override bool IsBuildMeshEmpty => Meshes.Count == 0;
		protected internal override bool IsDrawDataEmpty => MeshBufferData.Count == 0;

		private bool isDirty;

		[SetsRequiredMembers]
		public MutInterleavedModel() : base(new(), new()) {
			IfBuildEmpty = OnEmpty.SilentlyFail;
			BufferHint = BufferUsageHint.DynamicDraw;
		}

		public void AddMesh(TMesh mesh) {
			if (Meshes.Count >= byte.MaxValue) {
				Logger.Error($"Cannot add mesh because we are at mesh limit of {byte.MaxValue}. If you're seeing this then you should probably split up your models.");
				return;
			}

			Meshes.Add(mesh);
			isDirty = true;
		}

		public void Clear() {
			Meshes.Clear();
			isDirty = true;
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

		// TODO mutable methods
	}
}