using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public abstract class HoverableUiElement : UiElement {
		public bool IsHovered { get; private set; }

		protected event Action? OnHoverGainEvent;
		protected event Action? OnHoverLostEvent;

		protected HoverableUiElement(short x, short y, short z, ushort width, ushort height) : base(x, y, z, width, height) { }

		internal bool CheckForHover(ushort mouseX, ushort mouseY) => IsHovered = mouseX >= X && mouseY >= Y && mouseX <= X + Width && mouseY <= Y + Height;

		internal void InvokeHoverGain() => OnHoverGainEvent?.Invoke();
		internal void InvokeHoverLost() => OnHoverLostEvent?.Invoke();
	}
}