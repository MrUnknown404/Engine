using System.Diagnostics;
using Engine3.Exceptions;
using Engine3.Graphics.OpenGL;
using Engine3.Graphics.Vulkan;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics {
	public abstract class Window {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WindowHandle WindowHandle { get; }
		public Color4<Rgba> ClearColor { get; set; }
		public bool WasResized { get; set; }

		public event AttemptCloseWindow? TryCloseWindowEvent;
		public event Action? OnCloseWindowEvent;

		private bool wasCleanedUp;

		protected Window(WindowHandle windowHandle) => WindowHandle = windowHandle;

		public static Window MakeWindow(string title, uint width, uint height) {
			GraphicsApi graphicsApi = Engine3.GraphicsApi;
			if (graphicsApi == GraphicsApi.Console) { throw new Engine3Exception("Cannot create window when graphics api is set to console"); }

			Logger.Info("Making new window...");

			WindowHandle windowHandle = Toolkit.Window.Create(Engine3.GraphicsApiHints!); // if graphicsApi != GraphicsApi.Console then Engine3.GraphicsApiHints shouldn't be null here

			Toolkit.Window.SetTitle(windowHandle, title);
			Toolkit.Window.SetSize(windowHandle, new((int)width, (int)height));

			Window window = graphicsApi switch {
					GraphicsApi.OpenGL => GlWindow.MakeGlWindow(windowHandle),
					GraphicsApi.Vulkan => VkWindow.MakeVkWindow(windowHandle),
					GraphicsApi.Console => throw new UnreachableException(),
					_ => throw new ArgumentOutOfRangeException(),
			};

			Logger.Info("Window setup complete");

			Engine3.Windows.Add(window);
			return window;
		}

		public void TryCloseWindow() {
			if (wasCleanedUp) { return; }

			bool shouldClose = true;
			TryCloseWindowEvent?.Invoke(ref shouldClose);

			if (shouldClose) { CloseWindow(); }
		}

		public void CloseWindow(bool callEvent = true) {
			if (wasCleanedUp) { return; }

			if (callEvent) { OnCloseWindowEvent?.Invoke(); }
			Cleanup();
		}

		public void Show() => SetWindowMode(WindowMode.Normal);
		public void Hide() => SetWindowMode(WindowMode.Hidden);
		public void SetWindowMode(WindowMode windowMode) => Toolkit.Window.SetMode(WindowHandle, windowMode);

		private void Cleanup() {
			if (wasCleanedUp) {
				Logger.Warn("Attempted to cleanup a window that was already cleaned up");
				return;
			}

			CleanupGraphics();

			if (!Toolkit.Window.IsWindowDestroyed(WindowHandle)) { Toolkit.Window.Destroy(WindowHandle); } else { Logger.Warn("Tried to destroy an already destroyed window"); }

			wasCleanedUp = true;
		}

		protected abstract void CleanupGraphics();

		public delegate void AttemptCloseWindow(ref bool shouldCloseWindow);
	}
}