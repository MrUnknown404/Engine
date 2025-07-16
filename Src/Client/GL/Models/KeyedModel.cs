using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using USharpLibs.Common.IO;
using USharpLibs.Engine.Client.GL.Models.Vertex;

namespace USharpLibs.Engine.Client.GL.Models {
	[PublicAPI]
	public class KeyedModel<TKey, TVertex> : Model<TVertex, ICollection<Mesh<TVertex>>> where TVertex : IVertex where TKey : notnull {
		protected Dictionary<TKey, List<Mesh<TVertex>>> MeshesDict { get; } = new();
		protected override ICollection<Mesh<TVertex>> Meshes => MeshesDict.Values.SelectMany(ml => ml).ToList();
		protected bool IsDirty { get; set; }

		public KeyedModel(BufferUsageHint bufferHint) : base(bufferHint) { }

		public void AddMesh(TKey key, Mesh<TVertex> mesh) {
			if (!MeshesDict.TryGetValue(key, out List<Mesh<TVertex>>? meshList)) { MeshesDict[key] = meshList = new(); }
			meshList.Add(mesh);
			IsDirty = true;
		}

		public void AddMesh(TKey key, List<Mesh<TVertex>> mesh) {
			if (!MeshesDict.TryGetValue(key, out List<Mesh<TVertex>>? meshList)) { MeshesDict[key] = meshList = new(); }
			meshList.AddRange(mesh);
			IsDirty = true;
		}

		public void RemoveMesh(TKey key) {
			if (!MeshesDict.Remove(key)) {
				Logger.Warn("Attempted to remove an unknown key from KeyModel.");
				return;
			}

			IsDirty = true;
		}

		public void ClearMesh() {
			MeshesDict.Clear();
			IsDirty = true;
		}

		public bool ContainsKey(TKey key) => MeshesDict.ContainsKey(key);

		protected override void IDraw() {
			if (IsDirty) {
				BindModelData();
				IsDirty = false;
			}

			base.IDraw();
		}
	}
}