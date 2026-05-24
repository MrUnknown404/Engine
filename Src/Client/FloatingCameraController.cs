using System.Numerics;
using JetBrains.Annotations;
using OpenTK.Platform;

namespace Engine3.Client;

public class FloatingCameraController : CameraController {
	public float SensitivityX { get; set; } = 0.25f;
	public float SensitivityY { get; set; } = 0.25f;
	public float MaxPitch { get; set; } = 89;
	public float Speed { get; set; } = 0.1f;
	public float FastSpeed { get; set; } = 1f;

	public float Pitch { get; set => field = Math.Clamp(value, -MaxPitch, MaxPitch); } // TODO don't like this. i don't know how to clamp otherwise
	public float Yaw {
		get;
		set {
			field = value;
			if (field is > 360 or < -360) { field %= 360; }
		}
	}

	private readonly Window window;
	private Vector2 PreviousMousePosition { get; set; }
	private Vector2 MousePosition { get; set; }
	private bool isFirstMove;

	private bool isCursorLocked = true;

	public FloatingCameraController(Window window, Camera camera) : base(window.KeyboardManager, window.MouseManager, camera) {
		this.window = window;
		LockCursor();
	}

	public override void Update() {
		// lock check
		if (KeyboardManager.IsKey(Key.LeftAlt)) {
			if (isCursorLocked) { UnlockCursor(); }
		} else if (!isCursorLocked) { LockCursor(); }

		float rollDir = TranslateCamera();
		RotateCamera(rollDir);

		return;

		[MustUseReturnValue]
		float TranslateCamera() {
			Vector3 moveVector = new();
			bool fastSpeed = false;
			float rollDir = 0;

			if (KeyboardManager.IsKey(Key.W)) { moveVector += Camera.Forward; }
			if (KeyboardManager.IsKey(Key.A)) { moveVector += Camera.Left; }
			if (KeyboardManager.IsKey(Key.S)) { moveVector += Camera.Backwards; }
			if (KeyboardManager.IsKey(Key.D)) { moveVector += Camera.Right; }
			if (KeyboardManager.IsKey(Key.Space)) { moveVector += Vector3.UnitY; }
			if (KeyboardManager.IsKey(Key.LeftControl)) { moveVector += -Vector3.UnitY; }

			if (KeyboardManager.IsKey(Key.Q)) { rollDir += -1; }
			if (KeyboardManager.IsKey(Key.E)) { rollDir += 1; }

			if (KeyboardManager.IsKey(Key.LeftShift)) { fastSpeed = true; }

			if (moveVector != Vector3.Zero) { Camera.Position += Vector3.Normalize(moveVector) * (fastSpeed ? FastSpeed : Speed); }

			return rollDir;
		}

		void RotateCamera(float rollDir) {
			MousePosition = MouseManager.Position;

			if (!isCursorLocked) {
				PreviousMousePosition = MousePosition;
				return;
			}

			if (isFirstMove && PreviousMousePosition != MousePosition) {
				PreviousMousePosition = MousePosition;
				isFirstMove = false;
			}

			float mouseXOffset = MousePosition.X - PreviousMousePosition.X;
			float mouseYOffset = MousePosition.Y - PreviousMousePosition.Y;
			PreviousMousePosition = MousePosition;

			mouseXOffset *= SensitivityX;
			mouseYOffset *= SensitivityY;

			Pitch += mouseYOffset;
			Yaw += Camera.Up.Y < 0 ? -mouseXOffset : mouseXOffset;

			Quaternion qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(Pitch));
			Quaternion qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, float.DegreesToRadians(Yaw));
			Quaternion q = Quaternion.Normalize(qx * qy); // TODO roll

			Camera.Orientation = q;
			// Camera.RollDegrees += rollDir;
		}
	}

	public void UnlockCursor() {
		window.FreeCursorCapture();
		window.DefaultCursor();
		isCursorLocked = false;
	}

	public void LockCursor() {
		window.LockCursorCapture();
		window.HideCursor();
		isCursorLocked = true;
		isFirstMove = true;
	}
}