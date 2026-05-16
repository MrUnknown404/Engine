using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using ImGuiNET;
using JetBrains.Annotations;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client.Graphics.ImGui;

public unsafe class ImGuiBackend {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public nint Context { get; }
	public bool ShowDebugUI { get; set; }
	public IImGuiProvider? DebugUIImGui { get; set; }

	internal nint MouseWindowID { private get; set; }
	internal int MousePendingLeaveFrame { private get; set; }
	internal bool WantUpdateMonitors { private get; set; }

	private readonly Window window;
	private readonly Dictionary<WindowHandle, nint> windowToId = new();
	private readonly Queue<nint> freeWindowIdList = new();
	private readonly Action? showImGui;

	private nint nextFreeWindowId = 1;
	private ImGuiMouseCursor currentCursorType;

	public ImGuiBackend(Window window, _3DGraphicsApi graphicsBackend, Action? showImGui = null) {
		Logger.Debug("Setting up ImGui...");

		Context = ImGuiNet.CreateContext();
		this.window = window;
		this.showImGui = showImGui;

		window.OnResize += (_, _) => {
			ImGuiNet.SetCurrentContext(Context);
			ImGuiIOPtr io = ImGuiNet.GetIO();

			Vector2i frameBufferSize = window.GetFrameBufferSize();
			io.DisplaySize = new(frameBufferSize.X, frameBufferSize.Y); // TODO should this be window size or framebuffer size?
		};

		ImGuiNet.SetCurrentContext(Context);
		AddWindow(window);

		ImGuiIOPtr io = ImGuiNet.GetIO();
		ImGuiPlatformIOPtr platformIO = ImGuiNet.GetPlatformIO();
		ImGuiViewportPtr mainViewport = ImGuiNet.GetMainViewport();

		// io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
		io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
		io.ConfigFlags |= ImGuiConfigFlags.IsSRGB; // TODO what does this do? doesn't seem to do anything? use it?

		io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
		io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
		// io.BackendFlags |= ImGuiBackendFlags.HasMouseHoveredViewport;
		io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

		io.NativePtr->BackendRendererName = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Encoding.UTF8.GetBytes($"{Engine3.Name.ToLower()}_impl_{graphicsBackend.ToString().ToLower()}")));
		io.NativePtr->BackendPlatformName = io.NativePtr->BackendRendererName;

		io.Fonts.AddFontDefault();

		// platformIO.Renderer_RenderWindow = (nint)(delegate* unmanaged[Cdecl]<ImGuiViewportPtr, void>)&Renderer_RenderWindow;
		platformIO.Platform_SetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, void>)(&ImGuiH.Platform_SetClipboardText);
		platformIO.Platform_GetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<nint, byte*>)(&ImGuiH.Platform_GetClipboardText);

		mainViewport.PlatformHandle = GetWindowId(window.WindowHandle);

		ImGuiNet.StyleColorsDark();

		UpdateMonitors();

		// InitMultiViewportSupport(mainWindowId, emptyVao);

		EventQueue.EventRaised += OnEventQueueOnEventRaised;
	}

	[MustUseReturnValue]
	public bool NewFrame(out ImDrawDataPtr imDrawData) {
		ImGuiNet.SetCurrentContext(Context);
		ImGuiIOPtr io = ImGuiNet.GetIO();
		// io.DeltaTime = ; TODO set delta?

		if (WantUpdateMonitors) { UpdateMonitors(); }

		Toolkit.Mouse.GetGlobalMouseState(out MouseState mouseState);
		if (MousePendingLeaveFrame != 0 && MousePendingLeaveFrame >= ImGuiNet.GetFrameCount() && mouseState.PressedButtons == 0) {
			MouseWindowID = 0;
			MousePendingLeaveFrame = 0;
			io.AddMousePosEvent(float.MinValue, float.MinValue);
		}

		UpdateMouseData(window.WindowHandle);
		UpdateMouseCursor(window.WindowHandle);

		ImGuiNet.NewFrame();

		if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0) { ImGuiNet.DockSpaceOverViewport(0, null, ImGuiDockNodeFlags.PassthruCentralNode); }

		if (ShowDebugUI) {
			if (DebugUIImGui != null) { DebugUIImGui.ShowImGui(); } else { Logger.Warn("Trying to display DebugUI but we have no object"); }
		}

		showImGui?.Invoke();

		ImGuiNet.EndFrame();

		// if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0) {
		// 	ImGui.UpdatePlatformWindows();
		// 	ImGui.RenderPlatformWindowsDefault();
		// 	// Toolkit.OpenGL.SetCurrentContext(mainGLContext);
		// }

		ImGuiNet.Render();

		imDrawData = ImGuiNet.GetDrawData();
		return imDrawData is { Valid: true, CmdListsCount: > 0, };
	}

	[MustUseReturnValue] public bool IsOwner(WindowHandle windowHandle) => window.WindowHandle == windowHandle;
	[MustUseReturnValue] public nint GetWindowId(WindowHandle windowHandle) => windowToId[windowHandle];

	private void AddWindow(Window window) {
		nint windowId = freeWindowIdList.TryDequeue(out nint tempWindowId) ? tempWindowId : nextFreeWindowId++;
		if (windowToId.TryAdd(window.WindowHandle, windowId)) { Logger.Trace($"Added window ({windowId:X16})"); } else { Logger.Warn("Failed to add window. Duplicate"); }
	}

	private void RemoveWindow(Window window) {
		if (windowToId.Remove(window.WindowHandle, out nint windowId)) {
			Logger.Trace($"Removed window ({windowId:X16})");
			freeWindowIdList.Enqueue(windowId);
		} else { Logger.Warn("Failed to remove window. Not found"); }
	}

	private void UpdateMouseData(WindowHandle window) {
		ImGuiIOPtr io = ImGuiNet.GetIO();

		if (Toolkit.Window.IsFocused(window)) {
			if (io.WantSetMousePos) { Toolkit.Mouse.SetGlobalPosition((io.MousePos.X, io.MousePos.Y)); }
			// FIXME: Mouse passthrough...?
		}

		if ((io.BackendFlags & ImGuiBackendFlags.HasMouseHoveredViewport) != 0) {
			ImGuiViewportPtr imGuiViewport = ImGuiNet.FindViewportByPlatformHandle(MouseWindowID);
			io.AddMouseViewportEvent(imGuiViewport.NativePtr == null ? 0 : imGuiViewport.ID);
		}
	}

	private void UpdateMouseCursor(WindowHandle window) {
		ImGuiIOPtr io = ImGuiNet.GetIO();

		if (Toolkit.Window.GetCursorCaptureMode(window) == CursorCaptureMode.Locked) { return; }

		if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) { return; }

		ImGuiMouseCursor imGuiCursor = ImGuiNet.GetMouseCursor();
		if (io.MouseDrawCursor || imGuiCursor == ImGuiMouseCursor.None) {
			Toolkit.Window.SetCursor(window, null);
			return;
		}

		if (currentCursorType != imGuiCursor) {
			currentCursorType = imGuiCursor;
			Toolkit.Window.SetCursor(window, imGuiCursor switch {
					ImGuiMouseCursor.Arrow => Window.DefaultCursorHandle,
					ImGuiMouseCursor.TextInput => Window.TypingCursorHandle,
					ImGuiMouseCursor.ResizeAll => Window.ArrowFourWayCursorHandle,
					ImGuiMouseCursor.ResizeNS => Window.ArrowNSCursorHandle,
					ImGuiMouseCursor.ResizeEW => Window.ArrowEWCursorHandle,
					ImGuiMouseCursor.ResizeNESW => Window.ArrowNESWCursorHandle,
					ImGuiMouseCursor.ResizeNWSE => Window.ArrowNWSECursorHandle,
					ImGuiMouseCursor.Hand => Window.HandCursorHandle,
					ImGuiMouseCursor.NotAllowed => Window.ForbiddenCursorHandle,
					_ => Window.DefaultCursorHandle,
			});
		}
	}

	private void UpdateMonitors() {
		int displayCount = Toolkit.Display.GetDisplayCount();
		if (displayCount == 0) { throw new Engine3Exception("No displays found"); }

		ImGuiPlatformIOPtr platformIO = ImGuiNet.GetPlatformIO();
		if (platformIO.Monitors.Data != 0) { Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data); }

		platformIO.NativePtr->Monitors = new(displayCount, displayCount, Marshal.AllocHGlobal(displayCount * sizeof(ImGuiPlatformMonitor)));
		NativeMemory.Clear((void*)platformIO.Monitors.Data, (nuint)(platformIO.Monitors.Capacity * sizeof(ImGuiPlatformMonitor)));

		for (int i = 0; i < displayCount; i++) {
			ref ImGuiPlatformMonitor imGuiMonitor = ref Unsafe.Add(ref Unsafe.AsRef<ImGuiPlatformMonitor>((void*)platformIO.Monitors.Data), i);

			DisplayHandle displayHandle = Toolkit.Display.Open(i);
			Toolkit.Display.GetVirtualPosition(displayHandle, out int posX, out int posY);
			Toolkit.Display.GetResolution(displayHandle, out int resX, out int resY);
			Toolkit.Display.GetWorkArea(displayHandle, out Box2i workArea);
			Toolkit.Display.GetDisplayScale(displayHandle, out float scaleX, out _);

			imGuiMonitor.MainPos = new(posX, posY);
			imGuiMonitor.MainSize = new(resX, resY);

			imGuiMonitor.WorkPos = new(workArea.Min.X, workArea.Min.Y);
			imGuiMonitor.WorkSize = new(workArea.Size.X, workArea.Size.Y);
			imGuiMonitor.DpiScale = scaleX;
			imGuiMonitor.PlatformHandle = (void*)i;
		}

		WantUpdateMonitors = false;
	}

	public void Cleanup() {
		ImGuiNet.DestroyPlatformWindows();
		EventQueue.EventRaised -= OnEventQueueOnEventRaised;
	}

	private void OnEventQueueOnEventRaised(PalHandle? palHandle, PlatformEventType platformEventType, EventArgs args) => ImGuiH.EventQueue_EventRaised(this, args);
}