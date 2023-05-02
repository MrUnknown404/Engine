using JetBrains.Annotations;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public interface IRenderer {
		internal void SetupGL() {
			if (GameEngine.LoadState != LoadState.SetupGL) { throw new Exception($"Cannot setup IRenderer OpenGL during {GameEngine.LoadState}"); }
			ISetupGL();
		}

		public void ISetupGL();
		public void Render(double time);
	}
}