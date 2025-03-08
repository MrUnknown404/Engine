using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.IO;
using USharpLibs.Engine2.Client;
using USharpLibs.Engine2.Client.Shaders;
using USharpLibs.Engine2.Events;
using USharpLibs.Engine2.Exceptions;
using USharpLibs.Engine2.Init;
using USharpLibs.Engine2.Utils;

namespace USharpLibs.Engine2 {
	[SuppressMessage("ReSharper", "EventNeverSubscribedTo.Global")]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
	public abstract partial class GameEngine { // TODO  remove static from this class
		public static Version4 EngineVersion { get; } = new() { Release = 0, Major = 0, Minor = 0, };
		private static HashSet<Shader> AllShaders { get; } = new();

		/// <returns> Returns default before #Start is called. </returns>
		public static Version4 InstanceVersion { get; internal set; }

		[field: MaybeNull]
		public static Assembly EngineAssembly {
			get => field ?? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled);
			private set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		[field: MaybeNull]
		public static Assembly InstanceAssembly {
			get => field ?? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled);
			private set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		[field: MaybeNull]
		public static GameEngine Instance {
			get => field == null ? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled) : field;
			internal set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		[field: MaybeNull]
		public static EngineWindow WindowInstance {
			get => field == null ? throw new EngineStateException(EngineStateException.Reason.EngineStartNotCalled) : field;
			private set => field = field == null ? value : throw new EngineStateException(EngineStateException.Reason.EngineStartAlreadyCalled);
		}

		// FPS, Min. Max, Avg
		// TPS, Min. Max, Avg
		// also draw/tick times

		private static Stopwatch WatchUpdate { get; } = new();
		public static uint FPS { get; private set; } // TODO greatly expand upon this
		public static double FrameFrequency { get; private set; }

		public static bool IsCloseRequested {
			get {
				unsafe { return GLFW.WindowShouldClose(WindowInstance.WindowPtr) || field; }
			}
			private set;
		}

		protected static event EventResultDelegate? OnWindowReady;
		protected static event EventResultDelegate? OnOpenGLReady;
		protected static event EventResultDelegate? OnShadersReady;
		protected static event EventResultDelegate? OnSetupFinished;

		protected static event EventResultDelegate<OnRequestCloseEvent>? OnRequestClose;

		protected abstract void OnUpdate(double time);
		protected abstract void OnRender(double time);

		protected static void Start<TSelf>(StartupInfo info) where TSelf : GameEngine, new() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				IntPtr handle = GetStdHandle(unchecked((uint)-11)); // i have no idea where this magic number comes from
				GetConsoleMode(handle, out uint mode);
				SetConsoleMode(handle, mode | 0x0004);
			}

			Thread.CurrentThread.Name = "Main";
			Logger.Init($"Starting Client! Today is: {DateTime.Now:d/M/yyyy HH:mm:ss}");
			Logger.Debug($"Logs -> {Logger.LogDirectory}");

			Logger.Debug($"Engine is running version: {EngineVersion}");
			Logger.Debug($"Instance is running version: {info.Version}");

			Logger.Debug("Creating sources...");
			EngineAssembly = Assembly.GetAssembly(typeof(GameEngine)) ?? throw new NullReferenceException("Unable to get assembly for engine.");
			InstanceAssembly = Assembly.GetAssembly(typeof(TSelf)) ?? throw new NullReferenceException("Unable to get assembly for instance.");

			Logger.Debug("Creating self instance...");
			Instance = new TSelf();

			Logger.Debug("Creating window instance...");
			WindowInstance = new();
			InvokeEvent(OnWindowReady);

			Logger.Debug("Creating OpenGL window...");
			WindowInstance.CreateOpenGLWindow(info);
			Logger.Debug("Making sure OpenGL context is current...");
			WindowInstance.MakeContextCurrent(); // don't know if this is necessary

			Logger.Debug("OpenGL is now ready. Invoking events...");
			OpenGLReady(info);
			InvokeEvent(OnOpenGLReady);

			// TODO trace & time
			InitOpenGLObjects(info);
			InitEngineObjects();

			Logger.Debug("Engine finished initializing. Invoking events...");
			InvokeEvent(OnSetupFinished);
			Logger.Info("Engine initialization finished!");

			Logger.Debug("Starting game-loop...");
			EnterGameLoop();
			Logger.Info("Goodbye!");
		}

		private static void OpenGLReady(StartupInfo info) {
			Logger.Info("Setting up OpenGL!");
			Logger.Debug($"- OpenGL version: {GL.GetString(StringName.Version)}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}");

			if (info.AddOpenGLCallbacks) {
				Logger.Debug("- OpenGL Debug Flag is Enabled!");

				GL.Enable(EnableCap.DebugOutput);
				GL.Enable(EnableCap.DebugOutputSynchronous);

				uint[] ids = [
						131185, // Nvidia static buffer notification
				];

				GL.DebugMessageControl(DebugSourceControl.DebugSourceApi, DebugTypeControl.DebugTypeOther, DebugSeverityControl.DontCare, ids.Length, ids, false);

				GL.DebugMessageCallback(static (source, type, id, severity, length, message, _) => {
					switch (severity) {
						case DebugSeverity.DontCare: return;
						case DebugSeverity.DebugSeverityNotification:
							Logger.Debug($"OpenGL Notification: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Debug($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						case DebugSeverity.DebugSeverityHigh:
							Logger.Fatal($"OpenGL Fatal Error: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Fatal($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						case DebugSeverity.DebugSeverityMedium:
							Logger.Error($"OpenGL Error: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Error($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						case DebugSeverity.DebugSeverityLow:
							Logger.Warn($"OpenGL Warning: {id}. Source: {source.ToString()[11..]}, Type: {type.ToString()[9..]}");
							Logger.Warn($"- {Marshal.PtrToStringAnsi(message, length)}");
							break;
						default: throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
					}
				}, IntPtr.Zero);
			}

			GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
			GLH.EnableBlend(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GLH.EnableDepthTest();
			GLH.EnableCulling();
		}

		private static void InitOpenGLObjects(StartupInfo info) {
			// TODO setup loading screen
			// TODO setup fonts
			// TODO setup renderers

			// TODO time and trace

			SetupShaders(info);
			InvokeEvent(OnShadersReady);
		}

		private static void InitEngineObjects() {
			// TODO init
		}

		private static void SetupShaders(StartupInfo info) {
			Logger.Debug("Setting up shaders...");
			Stopwatch w = new();
			w.Start();

			HashSet<Shader> shaders = info.ShadersToRegister ?? new();

			AllShaders.UnionWith(DefaultShaders.AllShaders);
			AllShaders.UnionWith(shaders);

			foreach (Shader shader in AllShaders) { shader.SetupGL(); }

			w.Stop();
			Logger.Debug($"Setting up {AllShaders.Count} shaders took {(uint)w.ElapsedMilliseconds}ms");
		}

		private static void EnterGameLoop() {
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
			while (!IsCloseRequested) {
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

					WindowInstance.NewInputFrame();
					NativeWindow.ProcessWindowEvents(WindowInstance.IsEventDriven);

					GL.Clear(GLH.ClearBufferMask);

					Instance.OnUpdate(elapsed);
					Instance.OnRender(elapsed);

					WindowInstance.SwapBuffers();

					if (UpdatePeriod < WatchUpdate.Elapsed.TotalSeconds && slowUpdates <= MaxSlowUpdates) { slowUpdates++; } else if (slowUpdates > 0) { slowUpdates--; }
					if (WindowInstance.Api != ContextAPI.NoAPI && WindowInstance.VSync == VSyncMode.Adaptive) { GLFW.SwapInterval(slowUpdates > SlowUpdatesThreshold ? 0 : 1); }
				}

				double timeToNextUpdate = UpdatePeriod - WatchUpdate.Elapsed.TotalSeconds;
				if (timeToNextUpdate > 0) { OpenTK.Core.Utils.AccurateSleep(timeToNextUpdate, expectedSchedulerPeriod); }
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { timeEndPeriod(TimePeriod); }
		}

		public static void RequestClose(bool force = false) {
			if (force) {
				IsCloseRequested = true;
				Logger.Debug("Close requested forced.");
				return;
			}

			OnRequestCloseEvent result = InvokeEvent(OnRequestClose);
			Logger.Debug($"Close requested. ShouldClose: {result}");
			IsCloseRequested = result.ShouldClose;
		}

		private static void InvokeEvent(EventResultDelegate? e) => e?.Invoke();

		[MustUseReturnValue]
		private static T InvokeEvent<T>(EventResultDelegate<T>? e) where T : IEventResult, new() {
			if (e == null) { return (T)T.Empty; }

			T result = new();
			e.Invoke(result);
			return result;
		}

		protected delegate void EventResultDelegate();
		protected delegate void EventResultDelegate<in T>(T r) where T : IEventResult, new();

		[LibraryImport("Kernel32.dll", SetLastError = true)]
		private static partial void SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

		[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
		[LibraryImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

		[LibraryImport("Kernel32.dll", SetLastError = true)]
		private static partial IntPtr GetStdHandle(uint nStdHandle);

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

		public sealed class StartupInfo {
			public required Version4 Version { get; init; }
			public HashSet<Shader>? ShadersToRegister { get; init; }
			public EngineWindow.StartLocation StartLocation { get; init; } = EngineWindow.StartLocation.Default;
			public bool AddOpenGLCallbacks { get; init; }
			// TODO bool for adding default shaders
		}
	}
}