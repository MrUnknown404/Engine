namespace Engine3 {
	public static partial class Engine3 { // TODO impl
		public static bool WasOpenGLSetup { get; private set; }

		private static void SetupOpenGL() {
			throw new NotImplementedException(); // TODO opengl
			// WasOpenGLSetup = true;
		}

		private static void CleanupOpenGL() => throw new NotImplementedException(); // TODO opengl

		public class OpenGLSettings;
	}
}