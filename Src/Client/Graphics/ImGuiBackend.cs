using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using ImGuiNET;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client.Graphics {
	public abstract unsafe class ImGuiBackend {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected const string ImGuiName = "ImGui";

		public nint Context { get; }
		public Action? AddImGuiWindows { get; init; } = static () => ImGui.ShowDemoWindow();

		internal nint MouseWindowID { private get; set; }
		internal int MousePendingLeaveFrame { private get; set; }
		internal bool WantUpdateMonitors { private get; set; }

		private readonly Window window;
		private readonly Dictionary<WindowHandle, nint> windowToId = new();
		private readonly Queue<nint> freeWindowIdList = new();

		private nint nextFreeWindowId = 1;
		private SystemCursorType currentCursorType;

		internal ImGuiBackend(Window window, GraphicsBackend graphicsBackend) {
			Logger.Debug("Setting up ImGui...");

			Context = ImGui.CreateContext();
			this.window = window;

			ImGui.SetCurrentContext(Context);
			AddWindow(window);

			ImGuiIOPtr io = ImGui.GetIO();
			ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
			ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();

			// io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
			io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
			io.ConfigFlags |= ImGuiConfigFlags.IsSRGB; // TODO what does this do? use it?

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

			ImGui.StyleColorsDark();

			UpdateMonitors();

			// InitMultiViewportSupport(mainWindowId, emptyVao);

			EventQueue.EventRaised += OnEventQueueOnEventRaised;
		}

		public bool NewFrame(out ImDrawDataPtr imDrawData) {
			ImGui.SetCurrentContext(Context);

			ImGuiIOPtr io = ImGui.GetIO();

			Toolkit.Window.GetFramebufferSize(window.WindowHandle, out Vector2i frameBufferSize);
			io.DisplaySize = new(frameBufferSize.X, frameBufferSize.Y); // TODO set on change
			// io.DeltaTime = ; TODO set delta?

			if (WantUpdateMonitors) { UpdateMonitors(); }

			Toolkit.Mouse.GetGlobalMouseState(out MouseState mouseState);
			if (MousePendingLeaveFrame != 0 && MousePendingLeaveFrame >= ImGui.GetFrameCount() && mouseState.PressedButtons == 0) {
				MouseWindowID = 0;
				MousePendingLeaveFrame = 0;
				io.AddMousePosEvent(float.MinValue, float.MinValue);
			}

			UpdateMouseData(window.WindowHandle);
			UpdateMouseCursor(window.WindowHandle);

			ImGui.NewFrame();

			if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0) { ImGui.DockSpaceOverViewport(0, null, ImGuiDockNodeFlags.PassthruCentralNode); }

			AddImGuiWindows?.Invoke();

			ImGui.EndFrame();

			// if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0) {
			// 	ImGui.UpdatePlatformWindows();
			// 	ImGui.RenderPlatformWindowsDefault();
			// 	// Toolkit.OpenGL.SetCurrentContext(mainGLContext);
			// }

			ImGui.Render();

			imDrawData = ImGui.GetDrawData();
			return imDrawData is { Valid: true, CmdListsCount: > 0, };
		}

		public abstract void UpdateBuffers(ImDrawDataPtr drawData);

		public bool IsOwner(WindowHandle windowHandle) => window.WindowHandle == windowHandle;
		public nint GetWindowId(WindowHandle windowHandle) => windowToId[windowHandle];

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
			ImGuiIOPtr io = ImGui.GetIO();

			if (Toolkit.Window.IsFocused(window)) {
				if (io.WantSetMousePos) { Toolkit.Mouse.SetGlobalPosition((io.MousePos.X, io.MousePos.Y)); }
				// FIXME: Mouse passthrough...?
			}

			if ((io.BackendFlags & ImGuiBackendFlags.HasMouseHoveredViewport) != 0) {
				ImGuiViewportPtr imGuiViewport = ImGui.FindViewportByPlatformHandle(MouseWindowID);
				uint viewportID = imGuiViewport.NativePtr == null ? 0 : imGuiViewport.ID;
				io.AddMouseViewportEvent(viewportID);
			}
		}

		private void UpdateMouseCursor(WindowHandle window) {
			ImGuiIOPtr io = ImGui.GetIO();

			if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0) { return; }

			ImGuiMouseCursor imGuiCursor = ImGui.GetMouseCursor();
			if (io.MouseDrawCursor || imGuiCursor == ImGuiMouseCursor.None) {
				Toolkit.Window.SetCursor(window, null);
				return;
			}

			SystemCursorType cursorType = imGuiCursor switch {
					ImGuiMouseCursor.Arrow => SystemCursorType.Default,
					ImGuiMouseCursor.TextInput => SystemCursorType.TextBeam,
					ImGuiMouseCursor.ResizeAll => SystemCursorType.ArrowFourway,
					ImGuiMouseCursor.ResizeNS => SystemCursorType.ArrowNS,
					ImGuiMouseCursor.ResizeEW => SystemCursorType.ArrowEW,
					ImGuiMouseCursor.ResizeNESW => SystemCursorType.ArrowNESW,
					ImGuiMouseCursor.ResizeNWSE => SystemCursorType.ArrowNWSE,
					ImGuiMouseCursor.Hand => SystemCursorType.Hand,
					ImGuiMouseCursor.NotAllowed => SystemCursorType.Forbidden,
					_ => SystemCursorType.Default,
			};

			if (currentCursorType != cursorType) {
				currentCursorType = cursorType;
				Toolkit.Window.SetCursor(window, Toolkit.Cursor.Create(cursorType));
			}
		}

		private void UpdateMonitors() {
			int displayCount = Toolkit.Display.GetDisplayCount();
			if (displayCount == 0) { throw new Engine3Exception("No displays found"); }

			ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
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
			ImGui.DestroyPlatformWindows();
			EventQueue.EventRaised -= OnEventQueueOnEventRaised;
		}

		private void OnEventQueueOnEventRaised(PalHandle? palHandle, PlatformEventType platformEventType, EventArgs args) => ImGuiH.EventQueue_EventRaised(this, args);
	}

	public abstract class ImGuiBackend<T> : ImGuiBackend where T : IGraphicsResourceProvider {
		protected T GraphicsResourceProvider { get; }

		protected ImGuiBackend(Window window, GraphicsBackend graphicsBackend, T graphicsResourceProvider) : base(window, graphicsBackend) => GraphicsResourceProvider = graphicsResourceProvider;
	}
}