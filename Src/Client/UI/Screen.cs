using JetBrains.Annotations;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.UI.Elements;
using USharpLibs.Engine.Init;

namespace USharpLibs.Engine.Client.UI {
	[PublicAPI]
	public abstract class Screen {
		protected Dictionary<string, UiElement> Elements { get; } = new();
		protected HoverableUiElement? CurrentlyHovered { get; private set; }
		protected FocusableUiElement? CurrentlyFocused { get; private set; }
		protected ClickableUiElement? CurrentlyPressed { get; private set; }

		/// <summary> Adds a <see cref="UiElement"/> to the list of elements. </summary>
		/// <param name="key"> The internal key for the given element. This is so you can find a specific element later. </param>
		/// <param name="e"> The element to add. </param>
		/// <param name="replace"> Whether or not to replace an element if the given key is already present. </param>
		protected void AddElement(string key, UiElement e, bool replace = false) {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.Init) {
				Logger.Warn($"Cannot add UiElement during {GameEngine.CurrentLoadState}");
				return;
			}

			if (Elements.ContainsKey(key)) {
				if (replace) { Elements.Add(key, e); } else { Logger.Warn($"Duplicate key '{key}' found. If this was intentional set 'replace' to true"); }
				return;
			}

			Elements.Add(key, e);
		}

		internal void SetupGL() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new Exception($"Cannot setup Screen OpenGL during {GameEngine.CurrentLoadState}"); }
			ISetupGL();
		}

		/// <summary> Called at the start once the OpenGL context is created. Set up any OpenGL code here. </summary>
		protected virtual void ISetupGL() {
			foreach (UiElement e in Elements.Values) { e.SetupGL(); }
		}

		/// <summary> Called every frame. </summary>
		public virtual void Render(double time) {
			GLH.Bind(DefaultShaders.DefaultHud, s => {
				foreach (UiElement e in Elements.Values) {
					if (e.IsEnabled) {
						s.SetVector3("Position", new(e.X, e.Y, e.Z));
						e.Render(s, time);
					}
				}

				s.SetVector3("Position", new());
			});
		}

		internal bool CheckForPress(MouseButton button, ushort mouseX, ushort mouseY) {
			foreach (UiElement element in Elements.Values) {
				if (element is ClickableUiElement e && e.CheckForPress(mouseX, mouseY)) {
					if (e == CurrentlyPressed) { return true; }

					if (e.OnPress(button)) {
						CurrentlyPressed = e;
						return true;
					}
				}
			}

			CurrentlyPressed = null;
			return false;
		}

		internal bool CheckForRelease(MouseButton button, ushort mouseX, ushort mouseY) {
			if ((CurrentlyPressed?.CheckForRelease(mouseX, mouseY) ?? false) && (CurrentlyPressed?.OnRelease(button) ?? false)) {
				CurrentlyPressed = null;
				return true;
			}

			CurrentlyPressed?.OnReleaseFailed(button);
			CurrentlyPressed = null;
			return false;
		}

		internal void CheckForFocus(ushort mouseX, ushort mouseY) {
			foreach (UiElement element in Elements.Values) {
				if (element is FocusableUiElement e && e.CheckForFocus(mouseX, mouseY)) {
					if (e == CurrentlyFocused) { return; }

					CurrentlyFocused?.InvokeFocusLost();
					CurrentlyFocused = e;
					CurrentlyFocused.InvokeFocusGain();
					return;
				}
			}

			CurrentlyFocused?.InvokeFocusLost();
			CurrentlyFocused = null;
		}

		internal void CheckForHover(ushort mouseX, ushort mouseY) {
			foreach (UiElement element in Elements.Values) {
				if (element is HoverableUiElement e && e.CheckForHover(mouseX, mouseY)) {
					if (e == CurrentlyHovered) { return; }

					CurrentlyHovered?.InvokeHoverLost();
					CurrentlyHovered = e;
					CurrentlyHovered.InvokeHoverGain();
					return;
				}
			}

			CurrentlyHovered?.InvokeHoverLost();
			CurrentlyHovered = null;
		}
	}
}