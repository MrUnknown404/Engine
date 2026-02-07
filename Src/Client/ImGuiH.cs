using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Client.Graphics;
using Engine3.Exceptions;
using ImGuiNET;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client {
	public static unsafe class ImGuiH { // TODO setup multiple windows
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private static bool wantUpdateMonitors;
		private static IntPtr nativeClipboardText;

		private static nint mouseWindowID;
		private static int mousePendingLeaveFrame;
		private static SystemCursorType currentCursorType;

		private static readonly Dictionary<WindowHandle, nint> WindowToId = new();
		private static readonly Queue<nint> FreeWindowIdList = new();
		private static nint nextFreeWindowId = 1;

		internal static void Setup(GraphicsBackend graphicsBackend) {
			Logger.Debug($"- ImGui Version: {ImGui.GetVersion()}");
			ImGui.CreateContext();

			ImGuiIOPtr io = ImGui.GetIO();
			ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();

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
			platformIO.Platform_SetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, void>)(&Platform_SetClipboardText);
			platformIO.Platform_GetClipboardTextFn = (nint)(delegate* unmanaged[Cdecl]<nint, byte*>)(&Platform_GetClipboardText);

			ImGui.StyleColorsDark();

			wantUpdateMonitors = true;
			UpdateMonitors();

			// nint mainWindowId = RegisterWindow(mainWindowHandle, mainGLContextHandle); ???
			// ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
			// mainViewport.PlatformHandle = mainWindowId;

			// InitMultiViewportSupport(mainWindowId, emptyVao);

			EventQueue.EventRaised += EventQueue_EventRaised;
		}

		internal static void AddWindow(Window window) {
			if (!WindowToId.TryAdd(window.WindowHandle, FreeWindowIdList.TryDequeue(out nint idTemp) ? idTemp : nextFreeWindowId++)) { Logger.Warn("Failed to add window. Duplicate"); }
		}

		internal static void RemoveWindow(Window window) {
			if (WindowToId.Remove(window.WindowHandle, out nint windowId)) { FreeWindowIdList.Enqueue(windowId); } else { Logger.Warn("Failed to remove window. Not found"); }
		}

		public static void NewFrame(Window window) {
			ImGuiIOPtr io = ImGui.GetIO();

			Toolkit.Window.GetFramebufferSize(window.WindowHandle, out Vector2i framebufferSize);
			io.DisplaySize = new(framebufferSize.X, framebufferSize.Y); // TODO set on change
			// io.DeltaTime = ; TODO set delta?

			if (wantUpdateMonitors) { UpdateMonitors(); }

			Toolkit.Mouse.GetGlobalMouseState(out MouseState mouseState);
			if (mousePendingLeaveFrame != 0 && mousePendingLeaveFrame >= ImGui.GetFrameCount() && mouseState.PressedButtons == 0) {
				mouseWindowID = 0;
				mousePendingLeaveFrame = 0;
				io.AddMousePosEvent(float.MinValue, float.MinValue);
			}

			UpdateMouseData(window.WindowHandle);
			UpdateMouseCursor(window.WindowHandle);

			ImGui.NewFrame();

			if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0) { ImGui.DockSpaceOverViewport(0, null, ImGuiDockNodeFlags.PassthruCentralNode); }
		}

		public static void EndFrame() {
			ImGui.EndFrame();

			// if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0) {
			// 	ImGui.UpdatePlatformWindows();
			// 	ImGui.RenderPlatformWindowsDefault();
			// 	// Toolkit.OpenGL.SetCurrentContext(mainGLContext);
			// }
		}

		public static nint GetWindowId(WindowHandle windowHandle) => WindowToId[windowHandle];

		private static void UpdateMonitors() {
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

			wantUpdateMonitors = false;
		}

		private static void UpdateMouseData(WindowHandle window) {
			ImGuiIOPtr io = ImGui.GetIO();

			if (Toolkit.Window.IsFocused(window)) {
				if (io.WantSetMousePos) { Toolkit.Mouse.SetGlobalPosition((io.MousePos.X, io.MousePos.Y)); }
				// FIXME: Mouse passthrough...?
			}

			if ((io.BackendFlags & ImGuiBackendFlags.HasMouseHoveredViewport) != 0) {
				ImGuiViewportPtr imGuiViewport = ImGui.FindViewportByPlatformHandle(mouseWindowID);
				uint viewportID = imGuiViewport.NativePtr == null ? 0 : imGuiViewport.ID;
				io.AddMouseViewportEvent(viewportID);
			}
		}

		private static void UpdateMouseCursor(WindowHandle window) {
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

		private static void EventQueue_EventRaised(PalHandle? handle, PlatformEventType type, EventArgs args) {
			ImGuiIOPtr io = ImGui.GetIO();

			if (args is WindowEventArgs windowEvent && GetWindowId(windowEvent.Window) == 0) { return; }

			switch (args) {
				case MouseMoveEventArgs mouseMove: {
					float x = mouseMove.ClientPosition.X;
					float y = mouseMove.ClientPosition.Y;

					if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable)) {
						Toolkit.Window.ClientToScreen(mouseMove.Window, (x, y), out Vector2 screenPos);

						x = screenPos.X;
						y = screenPos.Y;
					}

					io.AddMousePosEvent(x, y);
					break;
				}
				case ScrollEventArgs scroll: io.AddMouseWheelEvent(scroll.Delta.X, scroll.Delta.Y); break;
				case MouseButtonDownEventArgs mouseDown: {
					if (mouseDown.Button is >= 0 and < (MouseButton)ImGuiMouseButton.COUNT) { io.AddMouseButtonEvent((int)mouseDown.Button, true); }
					break;
				}
				case MouseButtonUpEventArgs mouseUp: {
					if (mouseUp.Button is >= 0 and < (MouseButton)ImGuiMouseButton.COUNT) { io.AddMouseButtonEvent((int)mouseUp.Button, false); }
					break;
				}
				case TextInputEventArgs text: {
					nint utf8 = Marshal.StringToCoTaskMemUTF8(text.Text);
					ImGuiNative.ImGuiIO_AddInputCharactersUTF8(io.NativePtr, (byte*)utf8);
					Marshal.FreeCoTaskMem(utf8);
					break;
				}
				case KeyDownEventArgs keyDown: {
					UpdateKeyModifiers(keyDown.Modifiers);
					io.AddKeyEvent(TranslateKey(keyDown.Key), true);
					break;
				}
				case KeyUpEventArgs keyUp: {
					UpdateKeyModifiers(keyUp.Modifiers);
					io.AddKeyEvent(TranslateKey(keyUp.Key), false);
					break;
				}
				case DisplayConnectionChangedEventArgs: wantUpdateMonitors = true; break;
				case MouseEnterEventArgs mouseEnter: {
					if (mouseEnter.Entered) {
						mouseWindowID = GetWindowId(mouseEnter.Window);
						mousePendingLeaveFrame = 0;
					} else {
						// FIXME: Something about the pending frames...?
						//backendData.MousePendingLeaveFrame = ImGui.GetFrameCount() + 1;
					}

					break;
				}
				case FocusEventArgs focus: {
					io.AddFocusEvent(focus.GotFocus);
					break;
				}
				case WindowMoveEventArgs windowMove: {
					ImGui.FindViewportByPlatformHandle(GetWindowId(windowMove.Window)).PlatformRequestMove = true;
					break;
				}
				case WindowResizeEventArgs windowResize: {
					ImGui.FindViewportByPlatformHandle(GetWindowId(windowResize.Window)).PlatformRequestResize = true;
					break;
				}
				case CloseEventArgs windowClose: {
					ImGui.FindViewportByPlatformHandle(GetWindowId(windowClose.Window)).PlatformRequestClose = true;
					break;
				}
			}

			return;

			static void UpdateKeyModifiers(KeyModifier modifier) {
				ImGuiIOPtr io = ImGui.GetIO();
				io.AddKeyEvent(ImGuiKey.ModCtrl, modifier.HasFlag(KeyModifier.Control));
				io.AddKeyEvent(ImGuiKey.ModShift, modifier.HasFlag(KeyModifier.Shift));
				io.AddKeyEvent(ImGuiKey.ModAlt, modifier.HasFlag(KeyModifier.Alt));
				io.AddKeyEvent(ImGuiKey.ModSuper, modifier.HasFlag(KeyModifier.GUI));
			}

			static ImGuiKey TranslateKey(Key key) =>
					key switch {
							>= Key.D0 and <= Key.D9 => key - Key.D0 + ImGuiKey._0,
							>= Key.A and <= Key.Z => key - Key.A + ImGuiKey.A,
							>= Key.Keypad0 and <= Key.Keypad9 => key - Key.Keypad0 + ImGuiKey.Keypad0,
							>= Key.F1 and <= Key.F24 => key - Key.F1 + ImGuiKey.F24,
							Key.Tab => ImGuiKey.Tab,
							Key.LeftArrow => ImGuiKey.LeftArrow,
							Key.RightArrow => ImGuiKey.RightArrow,
							Key.UpArrow => ImGuiKey.UpArrow,
							Key.DownArrow => ImGuiKey.DownArrow,
							Key.PageUp => ImGuiKey.PageUp,
							Key.PageDown => ImGuiKey.PageDown,
							Key.Home => ImGuiKey.Home,
							Key.End => ImGuiKey.End,
							Key.Insert => ImGuiKey.Insert,
							Key.Delete => ImGuiKey.Delete,
							Key.Backspace => ImGuiKey.Backspace,
							Key.Space => ImGuiKey.Space,
							Key.Return => ImGuiKey.Enter,
							Key.Escape => ImGuiKey.Escape,
							Key.OEM7 => ImGuiKey.Apostrophe,
							Key.Comma => ImGuiKey.Comma,
							Key.Minus => ImGuiKey.Minus,
							Key.Period => ImGuiKey.Period,
							Key.OEM2 => ImGuiKey.Slash,
							Key.OEM1 => ImGuiKey.Semicolon,
							// FIXME: This is weird... we should do something about the key situation in PAL2.
							Key.Plus => ImGuiKey.Equal,
							Key.OEM4 => ImGuiKey.LeftBracket,
							Key.OEM5 => ImGuiKey.Backslash,
							Key.OEM6 => ImGuiKey.RightBracket,
							Key.OEM3 => ImGuiKey.GraveAccent,
							Key.CapsLock => ImGuiKey.CapsLock,
							Key.ScrollLock => ImGuiKey.ScrollLock,
							Key.NumLock => ImGuiKey.NumLock,
							Key.PrintScreen => ImGuiKey.PrintScreen,
							Key.PauseBreak => ImGuiKey.Pause,
							Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
							Key.KeypadDivide => ImGuiKey.KeypadDivide,
							Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
							Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
							Key.KeypadAdd => ImGuiKey.KeypadAdd,
							Key.KeypadEnter => ImGuiKey.KeypadEnter,
							Key.KeypadEqual => ImGuiKey.KeypadEqual,
							Key.LeftShift => ImGuiKey.LeftShift,
							Key.LeftControl => ImGuiKey.LeftCtrl,
							Key.LeftAlt => ImGuiKey.LeftAlt,
							Key.LeftGUI => ImGuiKey.LeftSuper,
							Key.RightShift => ImGuiKey.RightShift,
							Key.RightControl => ImGuiKey.RightCtrl,
							Key.RightAlt => ImGuiKey.RightAlt,
							Key.RightGUI => ImGuiKey.RightSuper,
							Key.Application => ImGuiKey.Menu,
							_ => ImGuiKey.None,
					};
		}

		// [UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl), ])]
		// private static void Platform_RenderWindow(ImGuiViewportPtr imGuiViewport, void* _) {
		// 	// Toolkit.OpenGL.SetCurrentContext(GetWindowInfo(imGuiViewport).GLContext);
		// }

		[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl), ])]
		private static void Platform_SetClipboardText(nint ctx, byte* text) => Toolkit.Clipboard.SetClipboardText(Marshal.PtrToStringUTF8((IntPtr)text) ?? throw new NullReferenceException());

		[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl), ])]
		private static byte* Platform_GetClipboardText(nint ctx) {
			if (nativeClipboardText != 0) { Marshal.FreeCoTaskMem(nativeClipboardText); }
			nativeClipboardText = Marshal.StringToCoTaskMemUTF8(Toolkit.Clipboard.GetClipboardText());
			return (byte*)nativeClipboardText;
		}

		public static void Cleanup() {
			ImGui.DestroyPlatformWindows();

			EventQueue.EventRaised -= EventQueue_EventRaised;

			if (nativeClipboardText != 0) { Marshal.FreeCoTaskMem(nativeClipboardText); }
		}

		// [UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl), ])]
		// private static void Renderer_RenderWindow(ImGuiViewportPtr viewport) {
		// 	// if ((viewport.Flags & ImGuiViewportFlags.NoRendererClear) != 0) {
		// 	// 	GL.ClearColor(0.1f, 0.1f, 0.1f, 1);
		// 	// 	GL.Clear(ClearBufferMask.ColorBufferBit);
		// 	// }
		// 	//
		// 	// RenderDrawData(viewport.DrawData);
		// }
	}
}