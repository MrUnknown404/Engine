using JetBrains.Annotations;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Client {
	[PublicAPI]
	public sealed class EngineWindow {
		public const string DefaultTitle = "Title";
		public const ushort DefaultWidth = 854, DefaultHeight = 480;

		private NativeWindow? OpenGLWindow { get; set; }

		public string Title {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.Title = Title; }
			}
		} = DefaultTitle;

		private short x;
		public short X {
			get => x;
			set {
				x = value;
				if (OpenGLWindow != null) { OpenGLWindow.Location = new(X, Y); }
			}
		}

		private short y;
		public short Y {
			get => y;
			set {
				y = value;
				if (OpenGLWindow != null) { OpenGLWindow.Location = new(X, Y); }
			}
		}

		public ushort Width {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.ClientSize = new(Width, Height); }
			}
		} = DefaultWidth;

		public ushort Height {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.ClientSize = new(Width, Height); }
			}
		} = DefaultHeight;

		public ushort MinWidth {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.MinimumSize = new(MinWidth, MinHeight); }
			}
		} = DefaultWidth;

		public ushort MinHeight {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.MinimumSize = new(MinWidth, MinHeight); }
			}
		} = DefaultHeight;

		public ushort MaxWidth {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.MaximumSize = new(MaxWidth, MaxHeight); }
			}
		} = ushort.MaxValue;

		public ushort MaxHeight {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.MaximumSize = new(MaxWidth, MaxHeight); }
			}
		} = ushort.MaxValue;

		public VSyncMode VSync {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.VSync = VSync; }
			}
		} = VSyncMode.Off;

		public WindowState WindowState {
			get;
			set {
				field = value;
				if (OpenGLWindow != null) { OpenGLWindow.WindowState = WindowState; }
			}
		} = WindowState.Normal;

		internal ContextAPI Api => (OpenGLWindow ?? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled)).API;
		internal bool IsEventDriven => (OpenGLWindow ?? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled)).IsEventDriven;
		internal unsafe Window* WindowPtr => (OpenGLWindow ?? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled)).WindowPtr;

		internal EngineWindow() { }

		internal void MakeContextCurrent() => OpenGLWindow!.Context.MakeCurrent();
		internal void NewInputFrame() => OpenGLWindow!.NewInputFrame();
		internal void SwapBuffers() => OpenGLWindow!.Context.SwapBuffers();

		internal void CreateOpenGLWindow(GameEngine.StartupInfo info) {
			OpenGLWindow = new(new() {
					Title = Title,
					Location = info.StartLocation == StartLocation.Position ? new(X, Y) : null,
					ClientSize = new(Width, Height),
					MinimumClientSize = new(MinWidth, MinHeight),
					MaximumClientSize = new(MaxWidth, MaxHeight),
					Vsync = VSync,
					WindowState = WindowState,
					StartVisible = false,
					Flags = info.AddOpenGLCallbacks ? ContextFlags.Debug : ContextFlags.Default,
			});

			if (info.StartLocation == StartLocation.Center) { OpenGLWindow.CenterWindow(); } else {
				x = (short)OpenGLWindow.Location.X;
				y = (short)OpenGLWindow.Location.Y;
			}

			OpenGLWindow.IsVisible = true;
		}

		public enum StartLocation : byte {
			Default = 0,
			Position,
			Center,
		}
	}
}