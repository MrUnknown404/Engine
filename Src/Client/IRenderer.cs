namespace USharpLibs.Engine.Client {
	public interface IRenderer {
		internal void SetupGL() {
			if (ClientBase.LoadState != LoadState.GL) { throw new Exception($"Cannot setup IRenderer OpenGL during {ClientBase.LoadState}"); }
			ISetupGL();
		}

		public void ISetupGL();
		public void Render(double time);
	}
}