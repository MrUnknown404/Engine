using JetBrains.Annotations;
using USharpLibs.Engine.Client.GL;

namespace USharpLibs.Engine.Client.UI.Elements {
	[PublicAPI]
	public abstract class UiElement {
		public short X { get; set; }
		public short Y { get; set; }
		public float Z { get; set; }

		public bool IsEnabled { get; set; } = true;

		protected UiElement(short x, short y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary> Called at the start once the OpenGL context is created. Set up any OpenGL code here. </summary>
		public abstract void SetupGL();

		/// <summary> By default, <see cref="Screen"/> calls this every frame. You can re-implement this if you wish. </summary>
		/// <param name="shader"> The shader the screen is using to render this. </param>
		/// <param name="time"> The time since the last frame was drawn. </param>
		public virtual void Render(Shader shader, double time) { }
	}
}