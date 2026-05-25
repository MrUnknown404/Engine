namespace Engine3.Client;

public abstract class CameraController {
	protected Camera Camera { get; }
	protected KeyboardManager KeyboardManager { get; }
	protected MouseManager MouseManager { get; }

	protected CameraController(KeyboardManager keyboardManager, MouseManager mouseManager, Camera camera) {
		Camera = camera;
		KeyboardManager = keyboardManager;
		MouseManager = mouseManager;
	}

	public abstract void Update();
	public abstract void Reset();
}