using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.IO;
using USharpLibs.Common.Utils;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public class GameWindow : NativeWindow {
		internal Queue<Action> CallOnMainThreadQueue { get; } = new();

		private Stopwatch WatchUpdate { get; } = new();

		public bool IsRunningSlowly { get; private set; }
		public double UpdateTime { get; protected set; }
		public double UpdateFrequency { get; }
		public int ExpectedSchedulerPeriod { get; set; } = 16;

		private int slowUpdates;

		public GameWindow(ushort updateFrequency, NativeWindowSettings nativeWindowSettings) : base(nativeWindowSettings) => UpdateFrequency = System.Math.Clamp(updateFrequency, 0d, 60d);

		public virtual unsafe void Run(GameEngine client) {
			const int TimePeriod = 8;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				SetThreadAffinityMask(GetCurrentThread(), 1);

#pragma warning disable CA1806
				timeBeginPeriod(TimePeriod);
#pragma warning restore CA1806
				ExpectedSchedulerPeriod = TimePeriod;
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
				ExpectedSchedulerPeriod = 1; //
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				ExpectedSchedulerPeriod = 1; //
			}

			Context?.MakeCurrent();

			Logger.Info("Running SetupGL...");
			Logger.Debug($"Running SetupGL took {TimeH.Time(() => {
				GameEngine.CurrentLoadState = GameEngine.LoadState.CreateGL;
				client.InvokeOnSetupGLEvent();
				GameEngine.CurrentLoadState = GameEngine.LoadState.SetupGL;
				client.InvokeOnSetupGLObjectsEvent();
			}).TotalMilliseconds:F1}ms");

			OnResize(new(ClientSize));

			Logger.Info("Running SetupEngine...");
			Logger.Debug($"Running SetupEngine took {TimeH.Time(() => {
				GameEngine.CurrentLoadState = GameEngine.LoadState.SetupEngine;
				client.InvokeOnSetupEngineEvent();
				client.InvokeOnSetupLoadingScreenEvent();
			}).TotalMilliseconds:F1}ms");

			Logger.Info("Running Setup...");
			Logger.Debug($"Running Setup took {TimeH.Time(() => {
				GameEngine.CurrentLoadState = GameEngine.LoadState.Setup;
				client.InvokeOnSetupEvent();
			}).TotalMilliseconds:F1}ms");

			Logger.Info("Running PostInit...");
			Logger.Debug($"Running PostInit took {TimeH.Time(() => {
				GameEngine.CurrentLoadState = GameEngine.LoadState.PostInit;
				client.InvokeOnPostInitEvent();
			}).TotalMilliseconds:F1}ms");

			Logger.Info("Running SetupFinished...");
			GameEngine.CurrentLoadState = GameEngine.LoadState.Done;
			client.InvokeOnSetupFinishedEvent();

			WatchUpdate.Start();
			while (!GLFW.WindowShouldClose(WindowPtr)) {
				if (CallOnMainThreadQueue.Count != 0) { CallOnMainThreadQueue.Dequeue().Invoke(); }

				double updatePeriod = UpdateFrequency == 0 ? 0 : 1 / UpdateFrequency;
				double elapsed = WatchUpdate.Elapsed.TotalSeconds;

				if (elapsed > updatePeriod) {
					WatchUpdate.Restart();

					NewInputFrame();
					ProcessWindowEvents(IsEventDriven);

					UpdateTime = elapsed;
					OnUpdateFrame(elapsed);
					OnRenderFrame(elapsed);

					const int MaxSlowUpdates = 80;
					const int SlowUpdatesThreshold = 45;

					if (updatePeriod < WatchUpdate.Elapsed.TotalSeconds) {
						slowUpdates++;
						if (slowUpdates > MaxSlowUpdates) { slowUpdates = MaxSlowUpdates; }
					} else {
						slowUpdates--;
						if (slowUpdates < 0) { slowUpdates = 0; }
					}

					IsRunningSlowly = slowUpdates > SlowUpdatesThreshold;
					if (API != ContextAPI.NoAPI && VSync == VSyncMode.Adaptive) { GLFW.SwapInterval(IsRunningSlowly ? 0 : 1); }
				}

				double timeToNextUpdate = updatePeriod - WatchUpdate.Elapsed.TotalSeconds;
				if (timeToNextUpdate > 0) { OpenTK.Core.Utils.AccurateSleep(timeToNextUpdate, ExpectedSchedulerPeriod); }
			}

			client.InvokeOnUnloadEvent();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#pragma warning disable CA1806
				timeEndPeriod(TimePeriod);
#pragma warning restore CA1806
			}
		}

		public virtual void SwapBuffers() {
			if (Context == null) { throw new InvalidOperationException("Cannot use SwapBuffers when running with ContextAPI.NoAPI."); }
			Context.SwapBuffers();
		}

		protected virtual void OnUpdateFrame(double time) { }
		protected virtual void OnRenderFrame(double time) { }

#pragma warning disable SYSLIB1054
		[DllImport("kernel32", SetLastError = true)]
		private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

		[DllImport("kernel32")] private static extern IntPtr GetCurrentThread();
		[DllImport("winmm")] private static extern uint timeBeginPeriod(uint uPeriod);
		[DllImport("winmm")] private static extern uint timeEndPeriod(uint uPeriod);
#pragma warning restore SYSLIB1054
	}
}