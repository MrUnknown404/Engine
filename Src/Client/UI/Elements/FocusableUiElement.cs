using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public abstract class FocusableUiElement : HoverableUiElement {
		public bool IsFocused { get; private set; }

		protected event Action? OnFocusGainEvent;
		protected event Action? OnFocusLostEvent;

		protected FocusableUiElement(short x, short y, float z, ushort width, ushort height) : base(x, y, z, width, height) { }

		internal bool CheckForFocus(ushort mouseX, ushort mouseY) => IsFocused = mouseX >= X && mouseY >= Y && mouseX <= X + Width && mouseY <= Y + Height;

		internal void InvokeFocusGain() => OnFocusGainEvent?.Invoke();
		internal void InvokeFocusLost() => OnFocusLostEvent?.Invoke();
	}
}