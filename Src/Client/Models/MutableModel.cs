using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Engine2.Client.Models.Vertex;

namespace USharpLibs.Engine2.Client.Models {
	[PublicAPI]
	public class MutableModel<TVertex> : ModelImpl<TVertex> where TVertex : IVertex {
		protected override List<Mesh<TVertex>> BuildMesh { get; } = new();
		protected override List<BufferData> BuiltMesh { get; } = new();

		protected internal override bool IsBuildMeshEmpty => BuildMesh.Count == 0;
		protected internal override bool IsDrawDataEmpty => BuiltMesh.Count == 0;

		private bool isDirty;

		public MutableModel() => IfBuildEmpty = OnEmpty.SilentlyFail;

		public void AddMesh(Mesh<TVertex> mesh) {
			if (BuildMesh.Count >= byte.MaxValue) {
				Logger.Error($"Cannot add mesh because we are at mesh limit of {byte.MaxValue}. If you're seeing this then you should probably split up your models.");
				return;
			}

			BuildMesh.Add(mesh);
			isDirty = true;
		}

		public void Clear() {
			BuildMesh.Clear();
			isDirty = true;
		}

		protected internal override void Build() {
			isDirty = false;

			foreach (BufferData data in BuiltMesh) { // Reuse buffers instead
				GL.DeleteBuffer(data.VBO);
				GL.DeleteBuffer(data.EBO);
			}

			BuiltMesh.Clear();

			foreach (Mesh<TVertex> mesh in BuildMesh) {
				uint vbo = (uint)GL.GenBuffer();
				uint ebo = (uint)GL.GenBuffer();

				BuiltMesh.Add(new() { VBO = vbo, EBO = ebo, Count = mesh.Indices.Length, });
				BindToBuffer(vbo, ebo, mesh.CollectVertices(), mesh.Indices);
			}
		}

		protected internal override bool CanBuild() => isDirty;
	}
}