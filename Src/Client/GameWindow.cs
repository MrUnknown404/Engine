using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine3.Client {
	public sealed class GameWindow {
		public const ushort DefaultWidth = 854, DefaultHeight = 480;

		private NativeWindow? openGLWindow;

		public string Title {
			get;
			set {
				field = value;
				openGLWindow?.Title = Title;
			}
		} = "Title";

		private short x;
		public short X {
			get => x;
			set {
				x = value;
				openGLWindow?.Location = new(X, Y);
			}
		}

		private short y;
		public short Y {
			get => y;
			set {
				y = value;
				openGLWindow?.Location = new(X, Y);
			}
		}

		public ushort Width {
			get;
			set {
				field = value;
				openGLWindow?.ClientSize = new(Width, Height);
			}
		} = DefaultWidth;

		public ushort Height {
			get;
			set {
				field = value;
				openGLWindow?.ClientSize = new(Width, Height);
			}
		} = DefaultHeight;

		public ushort MinWidth {
			get;
			set {
				field = value;
				openGLWindow?.MinimumSize = new(MinWidth, MinHeight);
			}
		} = DefaultWidth;

		public ushort MinHeight {
			get;
			set {
				field = value;
				openGLWindow?.MinimumSize = new(MinWidth, MinHeight);
			}
		} = DefaultHeight;

		public ushort MaxWidth {
			get;
			set {
				field = value;
				openGLWindow?.MaximumSize = new(MaxWidth, MaxHeight);
			}
		} = ushort.MaxValue;

		public ushort MaxHeight {
			get;
			set {
				field = value;
				openGLWindow?.MaximumSize = new(MaxWidth, MaxHeight);
			}
		} = ushort.MaxValue;

		public VSyncMode VSync {
			get;
			set {
				field = value;
				openGLWindow?.VSync = VSync;
			}
		} = VSyncMode.Off;

		public WindowState WindowState {
			get;
			set {
				field = value;
				openGLWindow?.WindowState = WindowState;
			}
		} = WindowState.Normal;

		public bool IsVisible {
			get;
			set {
				field = value;
				openGLWindow?.IsVisible = IsVisible;
			}
		}

		private unsafe Window* WindowPtr => (openGLWindow ?? throw new Exception()).WindowPtr; // TODO exception

		internal GameWindow() { }

		internal void MakeContextCurrent() => openGLWindow!.Context.MakeCurrent();

		internal void NewInputFrame() {
			openGLWindow!.NewInputFrame();
			NativeWindow.ProcessWindowEvents(openGLWindow.IsEventDriven);
		}

		internal void SwapBuffers() => openGLWindow!.Context.SwapBuffers();

		internal void CreateOpenGLWindow(StartLocation startLocation = StartLocation.Default, bool addOpenGLCallbacks = false) {
			openGLWindow = new(new() {
					Title = Title,
					Location = startLocation == StartLocation.Position ? new(X, Y) : null,
					ClientSize = new(Width, Height),
					MinimumClientSize = new(MinWidth, MinHeight),
					MaximumClientSize = new(MaxWidth, MaxHeight),
					Vsync = VSync,
					WindowState = WindowState,
					StartVisible = false,
					Flags = addOpenGLCallbacks ? ContextFlags.Debug : ContextFlags.Default,
			});

			if (startLocation == StartLocation.Center) {
				openGLWindow.CenterWindow(); //
			} else {
				x = (short)openGLWindow.Location.X;
				y = (short)openGLWindow.Location.Y;
			}
		}

		internal bool ShouldClose() {
			unsafe { return DoesWindowExist() && GLFW.WindowShouldClose(WindowPtr); }
		}

		public bool DoesWindowExist() => openGLWindow != null;

		public enum StartLocation : byte {
			Default = 0,
			Position,
			Center,
		}
	}
}