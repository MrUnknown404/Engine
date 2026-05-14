namespace Engine3.Client;

public abstract class CameraController {
	protected Camera Camera { get; }
	protected KeyboardManager KeyboardManager { get; }
	protected MouseManager MouseManager { get; }

	protected CameraController(Window window, Camera camera) {
		Camera = camera;
		KeyboardManager = window.KeyboardManager;
		MouseManager = window.MouseManager;
	}
}