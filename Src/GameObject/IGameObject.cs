namespace Engine3.GameObject {
	public interface IGameObject {
		public Guid Uuid { get; }
		public Scene.Scene Scene { get; }

		public void OnAddToScene();
		public void OnSceneRemove();
	}

	public interface IGameObject<out TTransform> : IGameObject where TTransform : ITransform<TTransform> {
		public TTransform Transform { get; }
	}
}