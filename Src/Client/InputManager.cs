using JetBrains.Annotations;
using OpenTK.Platform;

namespace Engine3.Client {
	public class InputManager {
		private readonly Dictionary<Key, bool> keys = new();

		public InputManager() {
			foreach (Key key in Enum.GetValues<Key>()) { keys[key] = false; }
		}

		internal void SetKey(Key key, bool value) => keys[key] = value;

		[Pure] public bool GetKey(Key key) => keys[key];
	}
}