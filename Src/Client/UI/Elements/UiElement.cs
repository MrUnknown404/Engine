using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public abstract class UiElement {
		public short X { get; set; }
		public short Y { get; set; }
		public float Z { get; set; }
		public ushort Width { get; set; }
		public ushort Height { get; set; }
		public bool IsEnabled { get; set; } = true;

		protected UiElement(short x, short y, float z, ushort width, ushort height) {
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

		public virtual void Render(Shader shader, double time) { }
	}
}