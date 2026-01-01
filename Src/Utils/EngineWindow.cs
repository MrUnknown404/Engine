using OpenTK.Platform;

namespace Engine3.Utils {
	public class EngineWindow {
		public WindowHandle WindowHandle { get; }
		public bool IsCloseRequested { get; private set; }

		public event AttemptCloseWindow? TryCloseWindowEvent;
		public event Action? OnCloseWindowEvent;

		public EngineWindow(WindowHandle windowHandle) => WindowHandle = windowHandle;

		public void TryCloseWindow() {
			bool shouldClose = true;
			TryCloseWindowEvent?.Invoke(ref shouldClose);

			if (shouldClose) { CloseWindow(); }
		}

		public void CloseWindow() {
			IsCloseRequested = true;
			OnCloseWindowEvent?.Invoke();
		}

		public void Show() => Toolkit.Window.SetMode(WindowHandle, WindowMode.Normal);
		public void Hide() => Toolkit.Window.SetMode(WindowHandle, WindowMode.Hidden);
		public void ExclusiveFullscreen() => Toolkit.Window.SetMode(WindowHandle, WindowMode.ExclusiveFullscreen);
		public void WindowedFullscreen() => Toolkit.Window.SetMode(WindowHandle, WindowMode.WindowedFullscreen);

		public delegate void AttemptCloseWindow(ref bool closeWindow);
	}
}