using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.UI.Elements;

namespace USharpLibs.Engine.Client.UI {
	public abstract class Screen {
		private Dictionary<string, UiElement> Elements { get; } = new();
		private HoverableUiElement? currentlyHovered;

		protected void AddElement(string key, UiElement e, bool replace = false) {
			if (ClientBase.LoadState != LoadState.Init) {
				Logger.Warn($"Cannot add UiElement during {ClientBase.LoadState}");
				return;
			}

			if (Elements.ContainsKey(key)) {
				if (replace) { Elements.Add(key, e); } else { Logger.Warn($"Duplicate key '{key}' found. If this was intentional set 'replace' to true"); }
				return;
			}

			Elements.Add(key, e);
		}

		internal void SetupGL() {
			if (ClientBase.LoadState != LoadState.GL) { throw new Exception($"Cannot setup Screen OpenGL during {ClientBase.LoadState}"); }
			ISetupGL();
		}

		protected virtual void ISetupGL() {
			foreach (UiElement e in Elements.Values) { e.SetupGL(); }
		}

		public virtual void Render(ExampleShader shader, double time) {
			foreach (UiElement e in Elements.Values) {
				if (e.IsEnabled) {
					shader.SetVector3("Position", new(e.X, e.Y, e.Z));
					e.Render(shader, time);
				}
			}

			shader.SetVector3("Position", new());
		}

		public void CheckForHover(ushort x, ushort y) {

			foreach (UiElement element in Elements.Values) {
				if (element is HoverableUiElement e && e.CheckForHover(x, y)) {
					if (e == currentlyHovered) { return; }

					currentlyHovered?.InvokeHoverLost();
					currentlyHovered = e;
					currentlyHovered.InvokeHoverGain();
					return;
				}
			}

			currentlyHovered?.InvokeHoverLost();
			currentlyHovered = null;
		}
	}
}