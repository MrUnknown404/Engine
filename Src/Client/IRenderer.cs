using JetBrains.Annotations;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public interface IRenderer {
		internal void SetupGL() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupEngine) { throw new($"Cannot setup IRenderer OpenGL during {GameEngine.CurrentLoadState}"); }
			ISetupGL();
		}

		/// <summary> Called at the start once the OpenGL context is created. Set up any OpenGL code here. </summary>
		public void ISetupGL();

		/// <summary> Called every time a frame is requested. </summary>
		/// <param name="time"> The time since the last frame was drawn. </param>
		public void Render(double time);

		/// <summary> Called 60 times a second. </summary>
		/// <param name="time"> The time since the last tick. </param>
		public void Tick(double time) { }
	}
}