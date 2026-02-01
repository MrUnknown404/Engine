namespace Engine3.Client.Graphics {
	public class Model {
		public Mesh[] Meshes { get; }

		public Model(Mesh[] meshes) => Meshes = meshes;
	}
}