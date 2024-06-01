using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Engine.Client.GL.Models.Vertex;

namespace USharpLibs.Engine.Client.GL.Models {
	[PublicAPI]
	public class MutableModel<TVertex> : Model<TVertex, List<Mesh<TVertex>>> where TVertex : IVertex {
		protected override List<Mesh<TVertex>> Meshes { get; } = new();
		protected bool IsDirty { get; set; }

		public MutableModel(BufferUsageHint bufferHint) : base(bufferHint) { }

		public MutableModel<TVertex> AddMesh(Mesh<TVertex> mesh, params Mesh<TVertex>[] meshes) {
			Meshes.Add(mesh);
			Meshes.AddRange(meshes);
			IsDirty = true;
			return this;
		}

		public MutableModel<TVertex> AddMesh(IEnumerable<Mesh<TVertex>> meshes) {
			Meshes.AddRange(meshes);
			IsDirty = true;
			return this;
		}

		public MutableModel<TVertex> SetMesh(Mesh<TVertex> mesh, params Mesh<TVertex>[] meshes) {
			ClearMesh();
			AddMesh(mesh, meshes);
			return this;
		}

		public MutableModel<TVertex> SetMesh(IEnumerable<Mesh<TVertex>> meshes) {
			AddMesh(meshes);
			return this;
		}

		public void ClearMesh() {
			Meshes.Clear();
			IsDirty = true;
		}

		protected override void IDraw() {
			if (IsDirty) { BindModelData(); }

			base.IDraw();
		}

		protected override bool BindModelData() {
			bool flag = base.BindModelData();
			if (flag) { IsDirty = false; }
			return flag;
		}
	}
}