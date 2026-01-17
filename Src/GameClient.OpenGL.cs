namespace Engine3 {
	public partial class GameClient {
		public bool WasOpenGLSetup { get; private set; }

		private void SetupOpenGL() { WasOpenGLSetup = true; }
		private void CleanupOpenGL() { }
	}
}