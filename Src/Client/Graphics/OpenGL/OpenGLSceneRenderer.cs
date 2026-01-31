namespace Engine3.Client.Graphics.OpenGL {
	public class OpenGLSceneRenderer : OpenGLRenderer, ISceneRenderer {
		public Scene.Scene? Scene { get; set; }

		protected OpenGLSceneRenderer(OpenGLGraphicsBackend graphicsBackend, OpenGLWindow window) : base(graphicsBackend, window) { }

		public override void Setup() { base.Setup(); }

		protected override void DrawFrame(float delta) {
			if (Scene != null) {
				// TODO draw our objects. how the hell is this going to work
			}
		}

		protected override void Cleanup() { }
	}
}