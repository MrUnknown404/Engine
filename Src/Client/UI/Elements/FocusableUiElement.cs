namespace USharpLibs.Engine.Client.UI.Elements {
	public abstract class FocusableUiElement : UiElement { // TODO
		public bool IsFocused { get; private set; }

		protected FocusableUiElement(short x, short y, short z, ushort width, ushort height) : base(x, y, z, width, height) { }

		internal bool CheckForFocus(ushort x, ushort y) => IsFocused = x >= X && y >= Y && x <= X + Width && y <= Y + Height;
	}
}