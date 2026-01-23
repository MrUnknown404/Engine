namespace Engine3 {
	public partial class GameClient {
		public uint[] DisabledCallbackIds { get; init; } = Array.Empty<uint>();
		public int SwapInterval { get; init; }

		public bool WasOpenGLSetup { get; private set; }

		private void SetupOpenGL() { WasOpenGLSetup = true; }
		private void CleanupOpenGL() { }
	}
}