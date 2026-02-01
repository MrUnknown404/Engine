using Engine3.GameObject;
using NLog;

namespace Engine3.Scene {
	public class Scene {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly List<IGameObject> gameObjects = new();

		public IEnumerable<IGameObject> GameObjects => gameObjects;

		public void AddGameObject<T>(T gameObject) where T : IGameObject {
			gameObjects.Add(gameObject);
			gameObject.OnAddToScene();
		}

		public void RemoveGameObject<T>(T gameObject) where T : IGameObject {
			if (!gameObjects.Remove(gameObject)) {
				Logger.Warn($"Failed to remove game object: {gameObject}");
				return;
			}

			gameObject.OnSceneRemove();
		}
	}
}