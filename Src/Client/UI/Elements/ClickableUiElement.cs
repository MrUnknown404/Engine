using JetBrains.Annotations;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public abstract class ClickableUiElement : HoverableUiElement {
		public bool IsHeldDown { get; private set; }

		protected ClickableUiElement(short x, short y, short z, ushort width, ushort height) : base(x, y, z, width, height) { }

		internal bool CheckForPress(ushort mouseX, ushort mouseY) => IsHeldDown = mouseX >= X && mouseY >= Y && mouseX <= X + Width && mouseY <= Y + Height;
		internal bool CheckForRelease(ushort mouseX, ushort mouseY) => mouseX >= X && mouseY >= Y && mouseX <= X + Width && mouseY <= Y + Height;

		protected internal abstract bool OnPress(MouseButton button);
		protected internal abstract bool OnRelease(MouseButton button);
		protected internal abstract void OnReleaseFailed(MouseButton button);
	}
}