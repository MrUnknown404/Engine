using System.Numerics;
using OpenTK.Platform;

namespace Engine3.Client {
	public class FloatingCameraController : CameraController {
		public float Sensitivity { get; set; } = 0.25f;
		public float MaxPitch { get; set; } = 89;
		public float Speed { get; set; } = 0.1f;
		public float FastSpeed { get; set; } = 1f;

		private readonly Window window;
		private Vector2 PreviousMousePosition { get; set; }
		private Vector2 MousePosition { get; set; }
		private bool isFirstMove;

		private readonly bool shouldLockCursor;
		private bool isCursorLocked = true;

		public FloatingCameraController(Window window, Camera camera, bool shouldLockCursor = true) : base(window, camera) {
			this.window = window;
			this.shouldLockCursor = shouldLockCursor;

			if (shouldLockCursor) { LockCursor(); }
		}

		public void Update() {
			// lock check
			if (shouldLockCursor) {
				if (KeyManager.IsKey(Key.LeftAlt)) {
					if (isCursorLocked) { UnlockCursor(); }
				} else if (!isCursorLocked) { LockCursor(); }
			}

			// # keyboard
			Vector3 moveVector = new();
			bool fastSpeed = false;

			// movement
			if (KeyManager.IsKey(Key.W)) { moveVector += Camera.Forward; }
			if (KeyManager.IsKey(Key.A)) { moveVector += -Camera.Right; }
			if (KeyManager.IsKey(Key.S)) { moveVector += -Camera.Forward; }
			if (KeyManager.IsKey(Key.D)) { moveVector += Camera.Right; }
			if (KeyManager.IsKey(Key.Space)) { moveVector += Vector3.UnitY; }
			if (KeyManager.IsKey(Key.LeftControl)) { moveVector += -Vector3.UnitY; }
			if (KeyManager.IsKey(Key.LeftShift)) { fastSpeed = true; }

			if (moveVector != Vector3.Zero) {
				moveVector = Vector3.Normalize(moveVector);
				Camera.Transform.Position += moveVector * (fastSpeed ? FastSpeed : Speed);
			}

			// # mouse
			MousePosition = MouseManager.Position;

			if (!isCursorLocked) {
				PreviousMousePosition = MousePosition;
				return;
			}

			if (isFirstMove && PreviousMousePosition != MousePosition) {
				PreviousMousePosition = MousePosition;
				isFirstMove = false;
			}

			// yaw/pitch
			float mouseXOffset = MousePosition.X - PreviousMousePosition.X;
			float mouseYOffset = MousePosition.Y - PreviousMousePosition.Y;
			PreviousMousePosition = MousePosition;

			mouseXOffset *= Sensitivity;
			mouseYOffset *= Sensitivity;

			Camera.YawDegrees += mouseXOffset;
			Camera.PitchDegrees -= mouseYOffset;

			if (Camera.PitchDegrees > MaxPitch || Camera.PitchDegrees < -MaxPitch) { Camera.PitchDegrees = Math.Clamp(Camera.PitchDegrees, -MaxPitch, MaxPitch); }
		}

		public void UnlockCursor() {
			window.FreeCursor();
			window.DefaultCursor();
			isCursorLocked = false;
		}

		public void LockCursor() {
			window.LockCursor();
			window.HideCursor();
			isCursorLocked = true;
			isFirstMove = true;
		}
	}
}