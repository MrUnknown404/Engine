using System.Numerics;
using JetBrains.Annotations;
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
			if (shouldLockCursor) { // lock check
				if (KeyManager.IsKey(Key.LeftAlt)) {
					if (isCursorLocked) { UnlockCursor(); }
				} else if (!isCursorLocked) { LockCursor(); }
			}

			float rollDir = TranslateCamera();
			RotateCamera(rollDir);

			return;

			[MustUseReturnValue]
			float TranslateCamera() {
				Vector3 moveVector = new();
				bool fastSpeed = false;
				rollDir = 0;

				if (KeyManager.IsKey(Key.W)) { moveVector += Camera.Forward; }
				if (KeyManager.IsKey(Key.A)) { moveVector += Camera.Left; }
				if (KeyManager.IsKey(Key.S)) { moveVector += Camera.Backwards; }
				if (KeyManager.IsKey(Key.D)) { moveVector += Camera.Right; }
				if (KeyManager.IsKey(Key.Space)) { moveVector += Camera.Up; }
				if (KeyManager.IsKey(Key.LeftControl)) { moveVector += Camera.Down; }

				if (KeyManager.IsKey(Key.Q)) { rollDir += -1; }
				if (KeyManager.IsKey(Key.E)) { rollDir += 1; }

				if (KeyManager.IsKey(Key.LeftShift)) { fastSpeed = true; }

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

				mouseXOffset *= Sensitivity;
				mouseYOffset *= Sensitivity;

				bool isUpsideDown = Camera.Up.Y < 0;

				Quaternion qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(mouseYOffset));
				Quaternion qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, float.DegreesToRadians(isUpsideDown ? -mouseXOffset : mouseXOffset));
				Quaternion q = Quaternion.Normalize(qx * Camera.Orientation * qy); // TODO roll

				// TODO limit qx

				Camera.Orientation = q;
				// Camera.RollDegrees += rollDir;
			}
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