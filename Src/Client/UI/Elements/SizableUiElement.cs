using JetBrains.Annotations;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public abstract class SizableUiElement : UiElement {
		public ushort Width { get; set; }
		public ushort Height { get; set; }

		protected SizableUiElement(short x, short y, float z, ushort width, ushort height) : base(x, y, z) {
			Width = width;
			Height = height;
		}
	}
}