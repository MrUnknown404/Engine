using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Client.UI.Elements {
	public abstract class UiElement {
		public short X { get; set; }
		public short Y { get; set; }
		public short Z { get; set; }
		public ushort Width { get; set; }
		public ushort Height { get; set; }
		public bool IsEnabled { get; set; } = true;

		protected UiElement(short x, short y, short z, ushort width, ushort height) {
			X = x;
			Y = y;
			Z = z;
			Width = width;
			Height = height;
		}

		internal void SetupGL() {
			if (GameEngine.LoadState != LoadState.SetupGL) { throw new Exception($"Cannot setup UiElement OpenGL during {GameEngine.LoadState}"); }
			ISetupGL();
		}

		protected abstract void ISetupGL();
		public abstract void Render(Shader shader, double time);
	}
}