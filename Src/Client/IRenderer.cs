using JetBrains.Annotations;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public interface IRenderer {
		internal void SetupGL() {
			if (GameEngine.CurrentLoadState != GameEngine.LoadState.SetupGL) { throw new Exception($"Cannot setup IRenderer OpenGL during {GameEngine.CurrentLoadState}"); }
			ISetupGL();
		}

		/// <summary> Called at the start once the OpenGL context is created. Set up any OpenGL code here. </summary>
		public void ISetupGL();

		/// <summary> Called every frame. </summary>
		public void Render(double time);
	}
}