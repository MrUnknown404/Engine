using Engine3.Client.Graphics;

namespace Engine3.GameObject {
	public class GameObject<TTransform> : IGameObject<TTransform> where TTransform : ITransform<TTransform> {
		public Guid Uuid { get; }
		public Scene.Scene Scene { get; }
		public TTransform Transform { get; } = TTransform.Zero;

		// TODO components? have transform be a component?

		public Model? Model { get; set; }

		protected GameObject(Guid uuid, Scene.Scene scene) {
			Uuid = uuid;
			Scene = scene;
		}

		public virtual void OnAddToScene() { }
		public virtual void OnSceneRemove() { }
	}
}