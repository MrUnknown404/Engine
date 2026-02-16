using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics;
using Engine3.Exceptions;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client {
	public abstract class Window : IEquatable<Window> {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> defaultCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.Default));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> crossCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.Cross));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> forbiddenCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.Forbidden));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> handCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.Hand));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> helpCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.Help));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> loadingCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.Loading));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> typingCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.TextBeam));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> waitCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.Wait));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> arrowNWSECursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.ArrowNWSE));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> arrowNESWCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.ArrowNESW));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> arrowEWCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.ArrowEW));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> arrowNSCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.ArrowNS));
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private static readonly Lazy<CursorHandle> arrowFourWayCursorHandle = new(static () => Toolkit.Cursor.Create(SystemCursorType.ArrowFourway));

		public static CursorHandle DefaultCursorHandle => defaultCursorHandle.Value;
		public static CursorHandle CrossCursorHandle => crossCursorHandle.Value;
		public static CursorHandle ForbiddenCursorHandle => forbiddenCursorHandle.Value;
		public static CursorHandle HandCursorHandle => handCursorHandle.Value;
		public static CursorHandle HelpCursorHandle => helpCursorHandle.Value;
		public static CursorHandle LoadingCursorHandle => loadingCursorHandle.Value;
		public static CursorHandle TypingCursorHandle => typingCursorHandle.Value;
		public static CursorHandle WaitCursorHandle => waitCursorHandle.Value;
		public static CursorHandle ArrowNWSECursorHandle => arrowNWSECursorHandle.Value;
		public static CursorHandle ArrowNESWCursorHandle => arrowNESWCursorHandle.Value;
		public static CursorHandle ArrowEWCursorHandle => arrowEWCursorHandle.Value;
		public static CursorHandle ArrowNSCursorHandle => arrowNSCursorHandle.Value;
		public static CursorHandle ArrowFourWayCursorHandle => arrowFourWayCursorHandle.Value;

		public WindowHandle WindowHandle { get; }
		public Color4<Rgba> ClearColor { get; set; } = new(0, 0, 0, 1);

		public KeyManager KeyManager { get; } = new();
		public MouseManager MouseManager { get; } = new();

		public bool ShouldClose { get; private set; }
		public bool WasResized { get; internal set; }
		public bool WasDestroyed { get; private set; }

		public event AttemptCloseWindow? TryCloseWindowEvent;
		public event Action? OnCloseWindowEvent;
		public event Action? BeforeDestroyEvent;
		public event Action<uint, uint>? OnResize;

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

		public void LockCursor() => SetCursorMode(CursorCaptureMode.Locked);
		public void ConfineCursor() => SetCursorMode(CursorCaptureMode.Confined);
		public void FreeCursor() => SetCursorMode(CursorCaptureMode.Normal);
		public void SetCursorMode(CursorCaptureMode cursorCaptureMode) => Toolkit.Window.SetCursorCaptureMode(WindowHandle, cursorCaptureMode);

		public void Show() => SetWindowMode(WindowMode.Normal);
		public void Hide() => SetWindowMode(WindowMode.Hidden);
		public void SetWindowMode(WindowMode windowMode) => Toolkit.Window.SetMode(WindowHandle, windowMode);

		public void HideCursor() => SetCursor(null);
		public void DefaultCursor() => SetCursor(DefaultCursorHandle);
		public void SetCursor(CursorHandle? cursor) => Toolkit.Window.SetCursor(WindowHandle, cursor);

		internal void Destroy() {
			if (WasDestroyed) {
				Logger.Warn($"Tried to destroy a {nameof(Window)} that was already destroyed");
				return;
			}

			if (defaultCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(DefaultCursorHandle); }
			if (crossCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(CrossCursorHandle); }
			if (forbiddenCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(ForbiddenCursorHandle); }
			if (handCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(HandCursorHandle); }
			if (helpCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(HelpCursorHandle); }
			if (loadingCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(LoadingCursorHandle); }
			if (typingCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(TypingCursorHandle); }
			if (waitCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(WaitCursorHandle); }
			if (arrowNWSECursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(ArrowNWSECursorHandle); }
			if (arrowNESWCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(ArrowNESWCursorHandle); }
			if (arrowEWCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(ArrowEWCursorHandle); }
			if (arrowNSCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(ArrowNSCursorHandle); }
			if (arrowFourWayCursorHandle.IsValueCreated) { Toolkit.Cursor.Destroy(ArrowFourWayCursorHandle); }

			Cleanup();

			if (!Toolkit.Window.IsWindowDestroyed(WindowHandle)) {
				BeforeDestroyEvent?.Invoke();
				Toolkit.Window.Destroy(WindowHandle);
			} else { Logger.Warn("Tried to destroy an already destroyed window"); }

			WasDestroyed = true;
		}

		protected abstract void Cleanup();

		public void InvokeOnResize(uint width, uint height) => OnResize?.Invoke(width, height);

		public delegate void AttemptCloseWindow(ref bool shouldCloseWindow);

		public bool Equals(Window? other) => other != null && WindowHandle.Equals(other.WindowHandle); // TODO replace with our own window id
		public override bool Equals(object? obj) => obj is Window window && Equals(window);

		public override int GetHashCode() => WindowHandle.GetHashCode();

		public static bool operator ==(Window? left, Window? right) => Equals(left, right);
		public static bool operator !=(Window? left, Window? right) => !Equals(left, right);
	}
}