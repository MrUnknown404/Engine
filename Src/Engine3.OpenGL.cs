namespace Engine3 {
	public static partial class Engine3 { // TODO impl
		public static bool WasOpenGLSetup { get; private set; }

		private static void SetupOpenGL() { WasOpenGLSetup = true; }

		private static void CleanupOpenGL() { }

		public class OpenGLSettings;
	}
}