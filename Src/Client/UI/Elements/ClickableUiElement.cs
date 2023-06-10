using JetBrains.Annotations;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public abstract class ClickableUiElement : HoverableUiElement {
		public bool IsHeldDown { get; private set; }

		protected ClickableUiElement(short x, short y, float z, ushort width, ushort height) : base(x, y, z, width, height) { }

		internal bool CheckForPress(ushort mouseX, ushort mouseY) => IsHeldDown = mouseX >= X && mouseY >= Y && mouseX <= X + Width && mouseY <= Y + Height;
		internal bool CheckForRelease(ushort mouseX, ushort mouseY) => mouseX >= X && mouseY >= Y && mouseX <= X + Width && mouseY <= Y + Height;

		/// <returns> Returns true if the click was successful. <br/> Otherwise, return false if the click was unsuccessful and the screen should keep trying to click other elements. </returns>
		protected internal abstract bool OnPress(MouseButton button);

		/// <returns> Returns true if the click was successful. <br/> Otherwise, return false if the click was unsuccessful and the screen should keep trying to click other elements. </returns>
		protected internal abstract bool OnRelease(MouseButton button);

		/// <summary> Called if this element was the last clicked element and the next click did not click this. </summary>
		protected internal abstract void OnReleaseFailed(MouseButton button);
	}
}