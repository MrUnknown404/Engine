using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Platform;

namespace Engine3.Client {
	public static unsafe class ImGuiH {
		private static IntPtr nativeClipboardText;

		public static void IndentedCollapsingHeader(string label, float indent, Action drawFunc, ImGuiTreeNodeFlags nodeFlags = ImGuiTreeNodeFlags.None) {
			if (ImGui.CollapsingHeader(label, nodeFlags)) {
				ImGui.Indent(indent);
				drawFunc();
				ImGui.Unindent(indent);
			}
		}

		public static void HelpMarker(string tooltip, bool sameLine = true) {
			if (sameLine) { ImGui.SameLine(); }

			ImGui.TextDisabled("(?)");
			if (ImGui.BeginItemTooltip()) {
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35);
				ImGui.TextUnformatted(tooltip);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}

		internal static void EventQueue_EventRaised(ImGuiBackend imGuiBackend, EventArgs args) {
			if (args is WindowEventArgs windowEvent && (!imGuiBackend.IsOwner(windowEvent.Window) || imGuiBackend.GetWindowId(windowEvent.Window) == 0)) { return; }

			ImGui.SetCurrentContext(imGuiBackend.Context);
			ImGuiIOPtr io = ImGui.GetIO();

			switch (args) {
				case MouseMoveEventArgs mouseMove:
					float x = mouseMove.ClientPosition.X;
					float y = mouseMove.ClientPosition.Y;

					if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable)) {
						Toolkit.Window.ClientToScreen(mouseMove.Window, (x, y), out Vector2 screenPos);

						x = screenPos.X;
						y = screenPos.Y;
					}

					io.AddMousePosEvent(x, y);
					break;
				case ScrollEventArgs scroll: io.AddMouseWheelEvent(scroll.Delta.X, scroll.Delta.Y); break;
				case MouseButtonDownEventArgs { Button: >= 0 and < (MouseButton)ImGuiMouseButton.COUNT, } mouseDown: io.AddMouseButtonEvent((int)mouseDown.Button, true); break;
				case MouseButtonUpEventArgs { Button: >= 0 and < (MouseButton)ImGuiMouseButton.COUNT, } mouseUp: io.AddMouseButtonEvent((int)mouseUp.Button, false); break;
				case TextInputEventArgs text:
					IntPtr utf8 = Marshal.StringToCoTaskMemUTF8(text.Text);
					ImGuiNative.ImGuiIO_AddInputCharactersUTF8(io.NativePtr, (byte*)utf8);
					Marshal.FreeCoTaskMem(utf8);
					break;
				case KeyDownEventArgs keyDown:
					UpdateKeyModifiers(keyDown.Modifiers);
					io.AddKeyEvent(TranslateKey(keyDown.Key), true);
					break;
				case KeyUpEventArgs keyUp:
					UpdateKeyModifiers(keyUp.Modifiers);
					io.AddKeyEvent(TranslateKey(keyUp.Key), false);
					break;
				case DisplayConnectionChangedEventArgs: imGuiBackend.WantUpdateMonitors = true; break;
				case MouseEnterEventArgs mouseEnter:
					if (mouseEnter.Entered) {
						imGuiBackend.MouseWindowID = imGuiBackend.GetWindowId(mouseEnter.Window);
						imGuiBackend.MousePendingLeaveFrame = 0;
					} else {
						// FIXME: Something about the pending frames...?
						//backendData.MousePendingLeaveFrame = ImGui.GetFrameCount() + 1;
					}

					break;
				case FocusEventArgs focus: io.AddFocusEvent(focus.GotFocus); break;
				case WindowMoveEventArgs windowMove: ImGui.FindViewportByPlatformHandle(imGuiBackend.GetWindowId(windowMove.Window)).PlatformRequestMove = true; break;
				case WindowResizeEventArgs windowResize:
					nint id = imGuiBackend.GetWindowId(windowResize.Window);
					ImGui.FindViewportByPlatformHandle(id).PlatformRequestResize = true;
					break;
				case CloseEventArgs windowClose: ImGui.FindViewportByPlatformHandle(imGuiBackend.GetWindowId(windowClose.Window)).PlatformRequestClose = true; break;
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

		[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl), ])]
		internal static void Platform_SetClipboardText(nint ctx, byte* text) => Toolkit.Clipboard.SetClipboardText(Marshal.PtrToStringUTF8((IntPtr)text) ?? throw new NullReferenceException());

		[UnmanagedCallersOnly(CallConvs = [ typeof(CallConvCdecl), ])]
		internal static byte* Platform_GetClipboardText(nint ctx) {
			if (nativeClipboardText != 0) { Marshal.FreeCoTaskMem(nativeClipboardText); }
			nativeClipboardText = Marshal.StringToCoTaskMemUTF8(Toolkit.Clipboard.GetClipboardText());
			return (byte*)nativeClipboardText;
		}

		public static void Cleanup() {
			if (nativeClipboardText != 0) { Marshal.FreeCoTaskMem(nativeClipboardText); }
		}
	}
}