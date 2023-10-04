using JetBrains.Annotations;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Utils;
using USharpLibs.Engine.Client.GL;
using USharpLibs.Engine.Client.UI.Elements;
using USharpLibs.Engine.Init;

namespace USharpLibs.Engine.Client.UI {
	[PublicAPI]
	public abstract class Screen {
		protected Dictionary<string, UiElement> Elements { get; } = new();
		protected Dictionary<string, TextElement> TextElements { get; } = new();

		protected HoverableUiElement? CurrentlyHovered { get; private set; }
		protected FocusableUiElement? CurrentlyFocused { get; private set; }
		protected ClickableUiElement? CurrentlyPressed { get; private set; }

		protected Screen() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.Init) { Logger.Warn($"Cannot create new Screen during {GameEngine.CurrentLoadState}. Use GameEngine#ScreenCreationEvent"); }
		}

		/// <summary> Adds a <see cref="UiElement"/> to the list of elements. </summary>
		/// <param name="key"> The internal key for the given element. This is so you can find a specific element later. </param>
		/// <param name="e"> The element to add. </param>
		/// <param name="replace"> Whether or not to replace an element if the given key is already present. </param>
		protected void AddElement(string key, UiElement e, bool replace = false) {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.Init) {
				Logger.Warn($"Cannot add UiElement during {GameEngine.CurrentLoadState}");
				return;
			}

			if (AllUiKeys().Contains(key)) {
				if (!replace) {
					Logger.Warn($"Duplicate key '{key}' found. If this was intentional set 'replace' to true");
					return;
				}
			}

			if (e is TextElement te) { TextElements.Add(key, te); } else { Elements.Add(key, e); }
		}

		internal void SetupGL() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new Exception($"Cannot setup Screen OpenGL during {GameEngine.CurrentLoadState}"); }
			ISetupGL();
		}

		/// <summary> Called at the start once the OpenGL context is created. Set up any OpenGL code here. </summary>
		protected virtual void ISetupGL() {
			foreach (UiElement e in AllUiElements()) { e.SetupGL(); }
		}

		/// <summary> Called 60 times a second. </summary>
		/// <param name="time"> The time since the last tick. </param>
		public virtual void Tick(double time) { }

		/// <summary> Called every time a frame is requested. </summary>
		/// <param name="time"> The time since the last frame was drawn. </param>
		public virtual void Render(double time) {
			GLH.Bind(DefaultShaders.DefaultHud, s => {
				foreach (UiElement e in Elements.Values.Where(e => e.IsEnabled)) {
					s.SetVector3("Position", new(e.X, e.Y, e.Z));
					e.Render(s, time);
				}

				s.SetVector3("Position", Vector3.Zero);
			});

			GLH.Bind(DefaultShaders.DefaultFont, s => {
				foreach (TextElement e in TextElements.Values.Where(e => e.IsEnabled && (e.DrawFont || e.DrawOutline))) {
					s.SetVector3("Position", new(e.X, e.Y, e.Z));
					s.SetBool("DrawFont", e.DrawFont);
					s.SetBool("DrawFont", e.DrawOutline);
					s.SetColor("FontColor", e.FontColor);
					s.SetColor("OutlineColor", e.OutlineColor);
					s.SetInt("OutlineSize", e.OutlineSize);

					e.Render(s, time);
				}

				s.SetVector3("Position", Vector3.Zero);
				s.SetBool("DrawFont", DefaultShaders.DefaultDrawFont);
				s.SetBool("DrawOutline", DefaultShaders.DefaultDrawOutline);
				s.SetColor("FontColor", DefaultShaders.DefaultFontColor);
				s.SetColor("OutlineColor", DefaultShaders.DefaultOutlineColor);
				s.SetInt("OutlineSize", DefaultShaders.DefaultOutlineSize);
			});
		}

		internal bool CheckForPress(MouseButton button, ushort mouseX, ushort mouseY) {
			foreach (UiElement element in AllUiElements()) {
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
			foreach (UiElement element in AllUiElements()) {
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
			foreach (UiElement element in AllUiElements()) {
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

		public List<string> AllUiKeys() {
			List<string> keys = new();
			keys.AddRange(Elements.Keys);
			keys.AddRange(TextElements.Keys);
			return keys;
		}

		public List<UiElement> AllUiElements() {
			List<UiElement> elements = new();
			elements.AddRange(Elements.Values);
			elements.AddRange(TextElements.Values);
			return elements;
		}
	}
}