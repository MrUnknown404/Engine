namespace Engine3.Client {
	public abstract class CameraController {
		protected Camera Camera { get; }
		protected KeyManager KeyManager { get; }
		protected MouseManager MouseManager { get; }

		protected CameraController(Window window, Camera camera) {
			Camera = camera;
			KeyManager = window.KeyManager;
			MouseManager = window.MouseManager;
		}
	}
}