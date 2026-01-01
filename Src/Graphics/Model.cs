using System.Numerics;

namespace Engine3.Graphics {
	public class Model { // TODO impl
		public Vector3 Position { get; init; }
		public Quaternion Rotation { get; init; }

		public List<Mesh> Meshes { get; } = new(); // How should this be stored?
	}
}