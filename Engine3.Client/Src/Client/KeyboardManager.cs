using OpenTK.Platform;

namespace Engine3.Client.Client;

public class KeyboardManager {
	private readonly Dictionary<Key, bool> keys = new();

	internal KeyboardManager() {
		foreach (Key key in Enum.GetValues<Key>()) { keys[key] = false; }
	}

	public bool IsKey(Key key) => keys[key];
	internal void SetKey(Key key, bool value) => keys[key] = value;
}