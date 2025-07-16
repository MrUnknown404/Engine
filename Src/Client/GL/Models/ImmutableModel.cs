using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine.Client.GL.Models.Vertex;

namespace USharpLibs.Engine.Client.GL.Models {
	/// <summary> *technically* not immutable </summary>
	[PublicAPI]
	public class ImmutableModel<TVertex> : Model<TVertex, Mesh<TVertex>[]> where TVertex : IVertex {
		protected override Mesh<TVertex>[] Meshes { get; }

		public ImmutableModel(Mesh<TVertex> mesh, params Mesh<TVertex>[] meshes) : base(BufferUsageHint.StaticDraw) {
			List<Mesh<TVertex>> listMeshes = new() { mesh, };
			listMeshes.AddRange(meshes);
			Meshes = listMeshes.ToArray();
		}

		public ImmutableModel(Mesh<TVertex>[] meshes) : base(BufferUsageHint.StaticDraw) => Meshes = meshes;
	}
}