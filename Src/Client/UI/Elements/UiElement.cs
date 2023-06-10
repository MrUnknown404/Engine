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
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new Exception($"Cannot setup UiElement OpenGL during {GameEngine.CurrentLoadState}"); }
			ISetupGL();
		}

		/// <summary> Called at the start once the OpenGL context is created. Set up any OpenGL code here. </summary>
		protected abstract void ISetupGL();

		/// <summary> By default, <see cref="Screen"/> calls this every frame. You can re-implement this if you wish. </summary>
		public virtual void Render(Shader shader, double time) { }
	}
}