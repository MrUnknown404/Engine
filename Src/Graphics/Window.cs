using System.Diagnostics;
using Engine3.Exceptions;
using Engine3.Graphics.OpenGL;
using Engine3.Graphics.Vulkan;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Graphics {
	public abstract class Window : IEquatable<Window> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WindowHandle WindowHandle { get; }
		public Color4<Rgba> ClearColor { get; set; } = new(0.01f, 0.01f, 0.01f, 1);
		public bool ShouldClose { get; private set; }
		public bool WasResized { get; internal set; }
		public bool WasDestroyed { get; private set; }

		public Renderer? Renderer {
			get;
			set {
				if (field is { WasDestroyed: false, }) { Logger.Warn("Window renderer was set without having destroyed the previous renderer. This will cause problems"); }
				field = value;
			}
		}

		public event AttemptCloseWindow? TryCloseWindowEvent;
		public event Action? OnCloseWindowEvent;
		public event Action? OnDestroyEvent;

		protected Window(WindowHandle windowHandle) => WindowHandle = windowHandle;

		public static Window MakeWindow(GameClient gameClient, string title, uint width, uint height) {
			GraphicsApi graphicsApi = gameClient.GraphicsApi;
			if (graphicsApi == GraphicsApi.Console) { throw new Engine3Exception("Cannot create window when graphics api is set to console"); }

			Logger.Info("Making new window...");

			WindowHandle windowHandle = Toolkit.Window.Create(gameClient.GraphicsApiHints!); // if graphicsApi != GraphicsApi.Console then GraphicsApiHints shouldn't be null here

			Toolkit.Window.SetTitle(windowHandle, title);
			Toolkit.Window.SetSize(windowHandle, new((int)width, (int)height));

			Window window = graphicsApi switch {
					GraphicsApi.OpenGL => GlWindow.MakeGlWindow(windowHandle),
					GraphicsApi.Vulkan => VkWindow.MakeVkWindow(gameClient, windowHandle),
					GraphicsApi.Console => throw new UnreachableException(),
					_ => throw new ArgumentOutOfRangeException(),
			};

			Logger.Info("Window setup complete");

			gameClient.Windows.Add(window);
			return window;
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

		internal void DestroyWindow() {
			if (WasDestroyed) {
				Logger.Warn("Attempted to destroy a window that was already destroyed");
				return;
			}

			Logger.Debug("Destroying window...");

			if (Renderer is { WasDestroyed: false, }) {
				Logger.Debug("Cleaning up window's renderer...");
				Renderer.TryCleanup();
			}

			Logger.Debug("Cleaning up window's graphics...");
			CleanupGraphics();

			if (!Toolkit.Window.IsWindowDestroyed(WindowHandle)) {
				OnDestroyEvent?.Invoke();

				bool successful = Engine3.GameInstance.Windows.Remove(this);
				if (!successful) { Logger.Warn("Could not find to be destroyed window in game client window list"); }

				Toolkit.Window.Destroy(WindowHandle);
			} else { Logger.Warn("Tried to destroy an already destroyed window"); }

			WasDestroyed = true;
		}

		protected abstract void CleanupGraphics();

		public delegate void AttemptCloseWindow(ref bool shouldCloseWindow);

		public bool Equals(Window? other) => other != null && WindowHandle.Equals(other.WindowHandle); // TODO replace with our own window id
		public override bool Equals(object? obj) => obj is Window window && Equals(window);

		public override int GetHashCode() => WindowHandle.GetHashCode();

		public static bool operator ==(Window? left, Window? right) => Equals(left, right);
		public static bool operator !=(Window? left, Window? right) => !Equals(left, right);
	}
}