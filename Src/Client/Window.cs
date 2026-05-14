using System.Diagnostics.CodeAnalysis;
using Engine3.Client.Graphics;
using Engine3.Exceptions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client;

[PublicAPI]
public abstract class Window : IEquatable<Window> { // TODO remove graphics specific versions of this class
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

	public WindowHandle WindowHandle { get; } // TODO protected
	public Color4<Rgba> ClearColor { get; set; } = new(0, 0, 0, 1);

	public abstract IGraphicsResourceProvider GraphicsResourceProvider { get; }

	public KeyboardManager KeyboardManager { get; } = new();
	public MouseManager MouseManager { get; } = new();

	public bool ShouldClose { get; private set; }
	/// <summary> Whether or not the window was resized &amp; needs to be handled. </summary>
	public bool WasResized { get; internal set; } // TODO there should be a proper way of handling this. atm it's handled per renderer which will cause problems if anyone wants to build their own
	public bool WasDestroyed { get; private set; }
	public bool IsHidden { get; private set; }

	/// <summary> Called when we request a window close. Setting shouldCloseWindow to false will ignore the request </summary>
	public event AttemptCloseWindowDelegate? TryCloseWindowEvent;
	/// <summary> Called when a window is to be closed on the next frame </summary>
	public event Action? OnCloseWindowEvent;
	public event Action? BeforeDestroyEvent;
	public event OnWindowResizeDelegate? OnResize;

	protected Window(EngineGraphicsBackend graphicsBackend, string title, uint width, uint height) {
		if (graphicsBackend.GraphicsBackend == GraphicsBackend.Console) { throw new Engine3Exception("Cannot create a window when graphics api is set to console"); }

		Logger.Info("Making new window...");
		WindowHandle = Toolkit.Window.Create(graphicsBackend.GraphicsApiHints!); // if graphicsApi != GraphicsApi.Console then GraphicsApiHints shouldn't be null here
		Toolkit.Window.SetTitle(WindowHandle, title);
		Toolkit.Window.SetSize(WindowHandle, new((int)width, (int)height));
	}

	/// <summary> Attempts to close the window. The application can decide whether or not to honor this request. If successful the window will close on the next frame </summary>
	public void TryCloseWindow() {
		if (WasDestroyed) { return; }

		bool shouldClose = true;
		TryCloseWindowEvent?.Invoke(ref shouldClose);

		if (shouldClose) { CloseWindow(); }
	}

	/// <summary> Forces the window to be destroyed next frame </summary>
	public void CloseWindow() {
		if (WasDestroyed) { return; }

		OnCloseWindowEvent?.Invoke();
		Logger.Debug("Close window requested. Destroying next frame");
		ShouldClose = true;
	}

	public void LockCursorCapture() => SetCursorCaptureMode(CursorCaptureMode.Locked);
	public void ConfineCursorCapture() => SetCursorCaptureMode(CursorCaptureMode.Confined);
	public void FreeCursorCapture() => SetCursorCaptureMode(CursorCaptureMode.Normal);
	public void SetCursorCaptureMode(CursorCaptureMode cursorCaptureMode) => Toolkit.Window.SetCursorCaptureMode(WindowHandle, cursorCaptureMode);

	public void Show() => SetWindowMode(WindowMode.Normal);
	public void Hide() => SetWindowMode(WindowMode.Hidden);
	public void SetWindowMode(WindowMode windowMode) => Toolkit.Window.SetMode(WindowHandle, windowMode);

	public void HideCursor() => SetCursor(null);
	public void DefaultCursor() => SetCursor(DefaultCursorHandle);

	/// <param name="cursor"> The new <see cref="CursorHandle"/> to use or null to hide the cursor </param>
	public void SetCursor(CursorHandle? cursor) => Toolkit.Window.SetCursor(WindowHandle, cursor);

	public Vector2i GetFrameBufferSize() {
		Toolkit.Window.GetFramebufferSize(WindowHandle, out Vector2i frameBufferSize);
		return frameBufferSize;
	}

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

	internal void OnCloseEventArgs() => TryCloseWindow();

	internal void OnResizeEventArgs(WindowResizeEventArgs resizeArgs) {
		WasResized = true;
		OnResize?.Invoke((uint)resizeArgs.NewClientSize.X, (uint)resizeArgs.NewClientSize.Y);
	}

	internal void OnModeChangeEventArgs(WindowModeChangeEventArgs modeArgs) {
		IsHidden = modeArgs.NewMode switch {
				WindowMode.Hidden or WindowMode.Minimized => true,
				WindowMode.Normal or WindowMode.Maximized or WindowMode.WindowedFullscreen or WindowMode.ExclusiveFullscreen => false,
				_ => throw new ArgumentOutOfRangeException(),
		};
	}

	internal void OnKeyDownEventArgs(KeyDownEventArgs downArgs) => KeyboardManager.SetKey(downArgs.Key, true);
	internal void OnKeyUpEventArgs(KeyUpEventArgs upArgs) => KeyboardManager.SetKey(upArgs.Key, false);
	internal void OnMouseMoveEventArgs(MouseMoveEventArgs moveArgs) => MouseManager.Position = new(moveArgs.ClientPosition.X, moveArgs.ClientPosition.Y);
	internal void OnMouseButtonDownEventArgs(MouseButtonDownEventArgs downArgs) => MouseManager.SetButton(downArgs.Button, true);
	internal void OnMouseButtonUpEventArgs(MouseButtonUpEventArgs upArgs) => MouseManager.SetButton(upArgs.Button, false);
	internal void OnScrollEventArgs(ScrollEventArgs scrollArgs) => MouseManager.ScrollDelta = scrollArgs.Delta.Y;

	public delegate void AttemptCloseWindowDelegate(ref bool shouldCloseWindow);
	public delegate void OnWindowResizeDelegate(uint width, uint height);

	public bool Equals(Window? other) => other != null && WindowHandle.Equals(other.WindowHandle); // TODO replace with our own window id
	public override bool Equals(object? obj) => obj is Window window && Equals(window);

	public override int GetHashCode() => WindowHandle.GetHashCode();

	public static bool operator ==(Window? left, Window? right) => Equals(left, right);
	public static bool operator !=(Window? left, Window? right) => !Equals(left, right);
}