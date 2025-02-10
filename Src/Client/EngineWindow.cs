using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Engine2.Exceptions;

namespace USharpLibs.Engine2.Client {
	[PublicAPI]
	public sealed partial class EngineWindow {
		public const string DefaultTitle = "Title";
		public const ushort DefaultWidth = 854, DefaultHeight = 480;

		private NativeWindow? openGLWindow;

		public string Title {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.Title = Title; }
			}
		} = DefaultTitle;

		public short X {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.ClientLocation = new(X, Y); }
			}
		}

		public short Y {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.ClientLocation = new(X, Y); }
			}
		}

		public ushort Width {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.ClientSize = new(Width, Height); }
			}
		} = DefaultWidth;

		public ushort Height {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.ClientSize = new(Width, Height); }
			}
		} = DefaultHeight;

		public ushort MinWidth {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.MinimumSize = new(MinWidth, MinHeight); }
			}
		} = DefaultWidth;

		public ushort MinHeight {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.MinimumSize = new(MinWidth, MinHeight); }
			}
		} = DefaultHeight;

		public ushort MaxWidth {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.MaximumSize = new(MaxWidth, MaxHeight); }
			}
		} = ushort.MaxValue;

		public ushort MaxHeight {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.MaximumSize = new(MaxWidth, MaxHeight); }
			}
		} = ushort.MaxValue;

		public VSyncMode VSync {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.VSync = VSync; }
			}
		} = VSyncMode.Off;

		public WindowState WindowState {
			get;
			set {
				field = value;
				if (openGLWindow != null) { openGLWindow.WindowState = WindowState; }
			}
		} = WindowState.Normal;

		public ClearBufferMask ClearBufferMask { get; set; } = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;
		public bool CenterOnCreate { private get; set; }

		internal unsafe Window* WindowPtr {
			get {
				if (openGLWindow == null) { throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled); }
				return openGLWindow.WindowPtr;
			}
		}

		private Stopwatch WatchUpdate { get; } = new();
		internal uint FPS { get; private set; }
		internal double FrameFrequency { get; private set; }

		internal EngineWindow() { }

		internal void MakeContextCurrent() {
			if (openGLWindow == null) { throw new NullReferenceException(); }
			openGLWindow.Context?.MakeCurrent();
		}

		internal void CreateOpenGLWindow() {
			openGLWindow = new(new() {
					Title = Title,
					Location = new(X, Y),
					ClientSize = new(Width, Height),
					MinimumClientSize = new(MinWidth, MinHeight),
					MaximumClientSize = new(MaxWidth, MaxHeight),
					Vsync = VSync,
					WindowState = WindowState,
					StartVisible = false,
			});

			if (CenterOnCreate) { openGLWindow.CenterWindow(); }

			openGLWindow.IsVisible = true;
		}

		internal void Run() {
			if (openGLWindow == null) { throw new NullReferenceException(); }

			const int TimePeriod = 8, MaxSlowUpdates = 80, SlowUpdatesThreshold = 45;
			const double UpdatePeriod = 1 / 60d;

			int expectedSchedulerPeriod = 16;

			//@formatter:off
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				SetThreadAffinityMask(GetCurrentThread(), 1);
				timeBeginPeriod(TimePeriod);
				expectedSchedulerPeriod = TimePeriod;
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
				expectedSchedulerPeriod = 1;
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				expectedSchedulerPeriod = 1;
			}
			//@formatter:on

			int slowUpdates = 0;
			uint frameCounter = 0;
			double frameTimeCounter = 0;

			WatchUpdate.Start();
			while (!GameEngine.IsCloseRequested) {
				double elapsed = WatchUpdate.Elapsed.TotalSeconds;
				if (elapsed > UpdatePeriod) {
					// this is close enough
					FrameFrequency = WatchUpdate.Elapsed.TotalMilliseconds;
					frameCounter++;

					if ((frameTimeCounter += elapsed) >= 1) {
						FPS = frameCounter;
						frameCounter = 0;
						frameTimeCounter -= 1;
					}

					WatchUpdate.Restart();

					openGLWindow.NewInputFrame();
					NativeWindow.ProcessWindowEvents(openGLWindow.IsEventDriven);

					GL.Clear(ClearBufferMask);

					GameEngine.Instance.OnUpdate(elapsed);
					GameEngine.Instance.OnRender(elapsed);

					openGLWindow.Context?.SwapBuffers();

					if (UpdatePeriod < WatchUpdate.Elapsed.TotalSeconds && slowUpdates <= MaxSlowUpdates) { slowUpdates++; } else if (slowUpdates > 0) { slowUpdates--; }
					if (openGLWindow.API != ContextAPI.NoAPI && openGLWindow.VSync == VSyncMode.Adaptive) { GLFW.SwapInterval(slowUpdates > SlowUpdatesThreshold ? 0 : 1); }
				}

				double timeToNextUpdate = UpdatePeriod - WatchUpdate.Elapsed.TotalSeconds;
				if (timeToNextUpdate > 0) { OpenTK.Core.Utils.AccurateSleep(timeToNextUpdate, expectedSchedulerPeriod); }
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { timeEndPeriod(TimePeriod); }
		}

		[LibraryImport("Kernel32", SetLastError = true)]
		private static partial void SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		[LibraryImport("Kernel32", SetLastError = true)]
		private static partial IntPtr GetCurrentThread();

		[LibraryImport("Winmm", SetLastError = true)]
		[SuppressMessage("ReSharper", "InconsistentNaming")] // windows func
		private static partial void timeBeginPeriod(uint uPeriod);

		[LibraryImport("Winmm", SetLastError = true)]
		[SuppressMessage("ReSharper", "InconsistentNaming")] // windows func
		private static partial void timeEndPeriod(uint uPeriod);

		// [DllImport("kernel32", SetLastError = true)]
		// private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		// [DllImport("kernel32")] private static extern IntPtr GetCurrentThread();
		// [DllImport("winmm")] private static extern uint timeBeginPeriod(uint uPeriod);
		// [DllImport("winmm")] private static extern uint timeEndPeriod(uint uPeriod);
	}
}