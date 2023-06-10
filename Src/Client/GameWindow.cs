using System.Diagnostics;
using JetBrains.Annotations;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public abstract class GameWindow : NativeWindow {
		internal Queue<Action> CallOnMainThreadQueue { get; } = new();

		protected event Action? Load;
		protected event Action? Unload;
		protected bool IsRunningSlowly { get; private set; }

		private Stopwatch WatchRender { get; } = new();
		private Stopwatch WatchUpdate { get; } = new();
		private double RenderFrequency { get; }
		private double UpdateFrequency { get; }
		private double updateEpsilon;

		protected GameWindow(byte maxFps, byte maxTps, NativeWindowSettings nativeWindowSettings) : base(nativeWindowSettings) {
			RenderFrequency = maxFps * 2; // Why does this need to be * 2??
			UpdateFrequency = maxTps;
		}

		public virtual unsafe void Run() {
			Context?.MakeCurrent();
			Load?.Invoke();
			OnResize(new(Size));

			WatchRender.Start();
			WatchUpdate.Start();

			while (!GLFW.WindowShouldClose(WindowPtr)) {
				if (CallOnMainThreadQueue.Count != 0) { CallOnMainThreadQueue.Dequeue()(); }

				double val1 = Math.Min(DispatchUpdateFrame(), DispatchRenderFrame());
				if (val1 > 0) { Thread.Sleep((int)Math.Floor(val1 * 1000)); }
			}

			Unload?.Invoke();
		}

		private double DispatchUpdateFrame() {
			int num1 = 4;
			double totalSeconds = WatchUpdate.Elapsed.TotalSeconds, num2;

			for (num2 = UpdateFrequency == 0 ? 0 : 1 / UpdateFrequency; totalSeconds > 0 && totalSeconds + updateEpsilon >= num2; totalSeconds = WatchUpdate.Elapsed.TotalSeconds) {
				ProcessInputEvents();
				ProcessWindowEvents(IsEventDriven);
				WatchUpdate.Restart();
				OnUpdateFrame(totalSeconds);
				updateEpsilon += totalSeconds - num2;

				if (UpdateFrequency > double.Epsilon) {
					IsRunningSlowly = updateEpsilon >= num2;

					if (IsRunningSlowly && --num1 == 0) {
						updateEpsilon = 0;
						break;
					}
				} else { break; }
			}

			return UpdateFrequency != 0 ? num2 - totalSeconds : 0;
		}

		private double DispatchRenderFrame() {
			double totalSeconds = WatchRender.Elapsed.TotalSeconds, num = RenderFrequency == 0 ? 0 : 1 / RenderFrequency;

			if (totalSeconds > 0 && totalSeconds >= num) {
				WatchRender.Restart();
				OnRenderFrame(totalSeconds);
				if (VSync == VSyncMode.Adaptive) { GLFW.SwapInterval(IsRunningSlowly ? 0 : 1); }
			}

			return RenderFrequency != 0 ? num - totalSeconds : 0;
		}

		protected void SwapBuffers() {
			if (Context == null) { throw new InvalidOperationException("Cannot use SwapBuffers when running with ContextAPI.NoAPI."); }
			Context.SwapBuffers();
		}

		protected abstract void OnUpdateFrame(double time);
		protected abstract void OnRenderFrame(double time);
	}
}