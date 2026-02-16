using System.Numerics;
using OpenTK.Platform;

namespace Engine3.Client {
	public class MouseManager {
		private readonly Dictionary<MouseButton, bool> buttons = new();

		public Vector2 Position { get; internal set; }
		public float ScrollDelta { get; internal set; }

		internal MouseManager() {
			foreach (MouseButton button in Enum.GetValues<MouseButton>()) { buttons[button] = false; }
		}

		public bool IsButton(MouseButton button) => buttons[button];
		internal void SetButton(MouseButton button, bool value) => buttons[button] = value;
	}
}