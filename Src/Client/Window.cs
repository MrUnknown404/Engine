using Engine3.Client.Graphics;
using Engine3.Exceptions;
using Engine3.Utility;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client {
	public abstract class Window : IDestroyable, IEquatable<Window> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WindowHandle WindowHandle { get; }
		public Color4<Rgba> ClearColor { get; set; } = new(0, 0, 0, 1);

		public InputManager InputManager { get; } = new();

		public bool ShouldClose { get; private set; }
		public bool WasResized { get; internal set; }
		public bool WasDestroyed { get; private set; }

		public event AttemptCloseWindow? TryCloseWindowEvent;
		public event Action? OnCloseWindowEvent;
		public event Action? BeforeDestroyEvent;

		protected Window(EngineGraphicsBackend graphicsBackend, string title, uint width, uint height) {
			if (graphicsBackend.GraphicsBackend == GraphicsBackend.Console) { throw new Engine3Exception("Cannot create a window when graphics api is set to console"); }

			Logger.Info("Making new window...");
			WindowHandle = Toolkit.Window.Create(graphicsBackend.GraphicsApiHints!); // if graphicsApi != GraphicsApi.Console then GraphicsApiHints shouldn't be null here
			Toolkit.Window.SetTitle(WindowHandle, title);
			Toolkit.Window.SetSize(WindowHandle, new((int)width, (int)height));
		}

		public void TryCloseWindow() {
			if (WasDestroyed) { return; }

			bool shouldClose = true;
			TryCloseWindowEvent?.Invoke(ref shouldClose);

			if (shouldClose) { CloseWindow(); }
		}

		public void CloseWindow() {
			if (WasDestroyed) { return; }

			OnCloseWindowEvent?.Invoke();
			Logger.Debug("Close window requested. Destroying next frame");
			ShouldClose = true;
		}

		public void Show() => SetWindowMode(WindowMode.Normal);
		public void Hide() => SetWindowMode(WindowMode.Hidden);
		public void SetWindowMode(WindowMode windowMode) => Toolkit.Window.SetMode(WindowHandle, windowMode);

		public void Destroy() {
			if (IDestroyable.WarnIfDestroyed(this)) { return; }

			Cleanup();

			if (!Toolkit.Window.IsWindowDestroyed(WindowHandle)) {
				BeforeDestroyEvent?.Invoke();
				Toolkit.Window.Destroy(WindowHandle);
			} else { Logger.Warn("Tried to destroy an already destroyed window"); }

			WasDestroyed = true;
		}

		protected abstract void Cleanup();

		public delegate void AttemptCloseWindow(ref bool shouldCloseWindow);

		public bool Equals(Window? other) => other != null && WindowHandle.Equals(other.WindowHandle); // TODO replace with our own window id
		public override bool Equals(object? obj) => obj is Window window && Equals(window);

		public override int GetHashCode() => WindowHandle.GetHashCode();

		public static bool operator ==(Window? left, Window? right) => Equals(left, right);
		public static bool operator !=(Window? left, Window? right) => !Equals(left, right);
	}
}