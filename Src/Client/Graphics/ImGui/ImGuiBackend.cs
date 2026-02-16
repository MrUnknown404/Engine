using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Engine3.Exceptions;
using Engine3.Utility;
using ImGuiNET;
using NLog;
using OpenTK.Mathematics;
using OpenTK.Platform;
using Vector2 = System.Numerics.Vector2;

namespace Engine3.Client.Graphics.ImGui {
	public abstract unsafe class ImGuiBackend {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected const string ImGuiName = "ImGui";

		public nint Context { get; }
		public Action? AddImGui { get; set; }
		public Action? AddExtraDebugUI { get; set; }
		public bool ShowDebugUI { get; set; }
		public byte IndentAmount { get; init; } = 6;

		internal nint MouseWindowID { private get; set; }
		internal int MousePendingLeaveFrame { private get; set; }
		internal bool WantUpdateMonitors { private get; set; }

		private readonly Window window;
		private readonly Dictionary<WindowHandle, nint> windowToId = new();
		private readonly Queue<nint> freeWindowIdList = new();

		private nint nextFreeWindowId = 1;
		private ImGuiMouseCursor currentCursorType;

		private bool showUpdateIndex;
		private bool showUps = true;
		private bool showUpdateTime = true;
		private bool showUpdateTimeGraph;
		private bool showMinMaxAvgUpdateTime;

		private bool showFrameIndex;
		private bool showFps = true;
		private bool showFrameTime = true;
		private bool showFrameTimeGraph;
		private bool showMinMaxAvgFrameTime;

		private bool showBackendSettings;
		private bool popoutUpdates;
		private bool popoutFrames;

		internal ImGuiBackend(Window window, GraphicsBackend graphicsBackend) {
			Logger.Debug("Setting up ImGui...");

			Context = ImGuiNet.CreateContext();
			this.window = window;

			ImGuiNet.SetCurrentContext(Context);
			AddWindow(window);

			ImGuiIOPtr io = ImGuiNet.GetIO();
			ImGuiPlatformIOPtr platformIO = ImGuiNet.GetPlatformIO();
			ImGuiViewportPtr mainViewport = ImGuiNet.GetMainViewport();

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

			ImGuiNet.StyleColorsDark();

			UpdateMonitors();

			// InitMultiViewportSupport(mainWindowId, emptyVao);

			EventQueue.EventRaised += OnEventQueueOnEventRaised;
		}

		public bool NewFrame(out ImDrawDataPtr imDrawData) {
			ImGuiNet.SetCurrentContext(Context);

			ImGuiIOPtr io = ImGuiNet.GetIO();

			Toolkit.Window.GetFramebufferSize(window.WindowHandle, out Vector2i frameBufferSize);
			io.DisplaySize = new(frameBufferSize.X, frameBufferSize.Y); // TODO set on change
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

			if (ShowDebugUI) { AddDebugUI(); }

			AddImGui?.Invoke();

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

		private void AddDebugUI() {
			GameClient game = Engine3.GameInstance;
			PerformanceMonitor pm = game.PerformanceMonitor;

			bool showAnyUpdates = showUpdateIndex || showUps || showUpdateTime || showMinMaxAvgUpdateTime;
			bool showAnyFrames = showFrameIndex || showFps || showFrameTime || showMinMaxAvgFrameTime;

			if (ImGuiNet.Begin("Debug")) {
				ImGuiH.IndentedCollapsingHeader("Performance", IndentAmount, ShowPerformance);
				ImGuiH.IndentedCollapsingHeader("Input", IndentAmount, ShowInput);
				AddExtraDebugUI?.Invoke();
			}

			ImGuiNet.End();

			if (showAnyUpdates && popoutUpdates) {
				ImGuiNet.Begin("Update Info");
				Show("Update", showUpdateIndex, showUps, showUpdateTime, showUpdateTimeGraph, showMinMaxAvgUpdateTime, game.UpdateIndex, pm.Ups, game.TargetUps, pm.UpdateTime, pm.LastUpdateTimes, pm.MinUpdateTime,
					pm.MaxUpdateTime, pm.AvgUpdateTime);

				ImGuiNet.End();
			}

			if (showAnyFrames && popoutFrames) {
				ImGuiNet.Begin("Frame Info");
				Show("Frame", showFrameIndex, showFps, showFrameTime, showFrameTimeGraph, showMinMaxAvgFrameTime, game.FrameIndex, pm.Fps, game.TargetFps, pm.FrameTime, pm.LastFrameTimes, pm.MinFrameTime, pm.MaxFrameTime,
					pm.AvgFrameTime);

				ImGuiNet.End();
			}

			return;

			void ShowInput() {
				ImGuiH.IndentedCollapsingHeader("Mouse", IndentAmount, ShowMouse);
				ImGuiH.IndentedCollapsingHeader("Keyboard", IndentAmount, ShowKeyboard);
			}

			void ShowMouse() {
				MouseManager mouseManager = window.MouseManager;

				Vector2 show = mouseManager.Position;
				ImGuiNet.InputFloat2("Position", ref show, "%.1f");
				ImGuiH.HelpMarker("X/Y");

				ImGuiNet.Text($"Scroll Delta: {mouseManager.ScrollDelta:F1}");

				foreach (MouseButton button in Enum.GetValues<MouseButton>()) {
					bool b = mouseManager.IsButton(button);
					ImGuiNet.Checkbox($"{button}", ref b);
				}
			}

			void ShowKeyboard() {
				KeyManager keyManager = window.KeyManager; // TODO show keyboard. active first then all?
				ImGuiNet.Text("Not implemented");
			}

			void ShowPerformance() {
				if (!popoutUpdates && showAnyUpdates) {
					ImGuiNet.SeparatorText("Update Info");
					Show("Update", showUpdateIndex, showUps, showUpdateTime, showUpdateTimeGraph, showMinMaxAvgUpdateTime, game.UpdateIndex, pm.Ups, game.TargetUps, pm.UpdateTime, pm.LastUpdateTimes, pm.MinUpdateTime,
						pm.MaxUpdateTime, pm.AvgUpdateTime);
				}

				if (!popoutFrames && showAnyFrames) {
					ImGuiNet.SeparatorText("Frame Info");
					Show("Frame", showFrameIndex, showFps, showFrameTime, showFrameTimeGraph, showMinMaxAvgFrameTime, game.FrameIndex, pm.Fps, game.TargetFps, pm.FrameTime, pm.LastFrameTimes, pm.MinFrameTime, pm.MaxFrameTime,
						pm.AvgFrameTime);
				}

				if (showBackendSettings) { ShowBackendSettings(); }

				if ((!popoutUpdates && showAnyUpdates) || (!popoutFrames && showAnyFrames) || showBackendSettings) { ImGuiNet.Separator(); }

				ImGuiH.IndentedCollapsingHeader("Toggles", IndentAmount, ShowToggles);
			}

			void Show(string name, bool showIndex, bool showPerSecond, bool showTime, bool showTimeGraph, bool showMinMaxAvgTime, ulong index, uint perSecond, uint targetPerSecond, float time, float[] times, float minTime,
				float maxTime, float avgTime) {
				if (showIndex) { ImGuiNet.Text($"Index: {index}"); }
				if (showPerSecond) { ImGuiNet.Text($"{name[0]}ps: {perSecond}{(targetPerSecond == 0 ? string.Empty : $"/{targetPerSecond}")}"); }
				if (showTime) { ImGuiNet.Text($"Time: {time:F3} ms"); }

				if (showTimeGraph) {
					if (pm.StoreTimesForGraph) {
						if (times.Length != 0) { ImGuiNet.PlotLines($"{name} Time Graph", ref times[0], times.Length); }
					} else { ImGuiNet.Text($"{nameof(pm.StoreTimesForGraph)} is false"); }
				}

				if (showMinMaxAvgTime) {
					if (pm.CalculateMinMaxAverage) {
						ImGuiNet.Text($"Min: {minTime:F3} ms");
						ImGuiNet.Text($"Max: {maxTime:F3} ms");
						ImGuiNet.Text($"Avg: {avgTime:F3} ms");
					} else { ImGuiNet.Text($"{nameof(pm.CalculateMinMaxAverage)} is false"); }
				}
			}

			void ShowBackendSettings() {
				ImGuiNet.SeparatorText("Backend Settings");

				ImGuiNet.Text($"Calculate Min/Max/Avg: {pm.CalculateMinMaxAverage}");
				ImGuiNet.Text($"Min/Max/Avg Sample Time: {pm.MinMaxAverageSampleTime} seconds");
				ImGuiNet.Text($"Store Times For Graph: {pm.StoreTimesForGraph}");
				ImGuiNet.Text($"Update Time Graph Size: {pm.UpdateTimeGraphSize}");
				ImGuiNet.Text($"Frame Time Graph Size: {pm.FrameTimeGraphSize}");
			}

			void ShowToggles() {
				ImGuiNet.Checkbox("Show Update Index", ref showUpdateIndex);
				ImGuiNet.Checkbox("Show Ups", ref showUps);
				ImGuiNet.Checkbox("Show Update Time", ref showUpdateTime);
				ImGuiNet.Checkbox("Show Update Time Graph", ref showUpdateTimeGraph);
				ImGuiNet.Checkbox("Show Min/Max/Avg Update Time", ref showMinMaxAvgUpdateTime);

				ImGuiNet.Separator();
				ImGuiNet.Checkbox("Show Frame Index", ref showFrameIndex);
				ImGuiNet.Checkbox("Show Fps", ref showFps);
				ImGuiNet.Checkbox("Show Frame Time", ref showFrameTime);
				ImGuiNet.Checkbox("Show Frame Time Graph", ref showFrameTimeGraph);
				ImGuiNet.Checkbox("Show Min/Max/Avg Frame Time", ref showMinMaxAvgFrameTime);

				ImGuiNet.Separator();
				ImGuiNet.Checkbox("Show Backend Settings", ref showBackendSettings);
				ImGuiNet.Checkbox("Popout Updates", ref popoutUpdates);
				ImGuiNet.Checkbox("Popout Frames", ref popoutFrames);
			}
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

	public abstract class ImGuiBackend<T> : ImGuiBackend where T : IGraphicsResourceProvider {
		protected T GraphicsResourceProvider { get; }

		protected ImGuiBackend(Window window, GraphicsBackend graphicsBackend, T graphicsResourceProvider) : base(window, graphicsBackend) => GraphicsResourceProvider = graphicsResourceProvider;
	}
}