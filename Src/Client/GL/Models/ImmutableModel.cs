using JetBrains.Annotations;
using USharpLibs.Engine2.Client.GL.Models.Vertex;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine2.Client.GL.Models {
	[PublicAPI]
	public class ImmutableModel<TVertex> : ModelImpl<TVertex> where TVertex : IVertex {
		protected override Mesh<TVertex>[] BuildMesh { get; }
		protected override BufferData[] BuiltMesh { get; }

		protected internal override bool IsBuildMeshEmpty => BuildMesh.Length == 0;
		protected internal override bool IsDrawDataEmpty => !wasBuilt;

		private bool wasBuilt;

		public ImmutableModel(Mesh<TVertex>[] buildMesh) {
			BuildMesh = buildMesh;
			BuiltMesh = new BufferData[buildMesh.Length];
		}

		protected internal override void Build() {
			wasBuilt = true;

			for (int i = 0; i < BuildMesh.Length; i++) {
				Mesh<TVertex> mesh = BuildMesh[i];
				uint vbo = (uint)OpenGL4.GenBuffer();
				uint ebo = (uint)OpenGL4.GenBuffer();

				BuiltMesh[i] = new() { VBO = vbo, EBO = ebo, Count = mesh.Indices.Length, };
				BindToBuffer(vbo, ebo, mesh.CollectVertices(), mesh.Indices);
			}
		}

		protected internal override bool CanBuild() {
			if (wasBuilt) { throw new InvalidOperationException($"Attempted to build an already built {nameof(ImmutableModel<TVertex>)}."); }
			return true;
		}
	}
}