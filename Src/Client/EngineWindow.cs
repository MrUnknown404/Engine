using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public sealed class EngineWindow : GameWindow {
		private ClientBase Client { get; }

		private uint frameCounter, tickCounter;
		private double frameTimeCounter, tickTimeCounter;

		public EngineWindow(ClientBase client) : base(GameWindowSettings.Default,
				new NativeWindowSettings {
						MinimumSize = new(client.MinWidth, client.MinHeight),
						MaximumSize = client.MaxWidth == 0 || client.MaxHeight == 0 ? null : new(client.MaxWidth, client.MaxHeight),
						Size = new(client.MinWidth, client.MinHeight),
						Title = client.OriginalTitle,
				}) {
			Client = client;
			UpdateFrequency = 60;

			Load += () => {
				ClientBase.LoadState = LoadState.GL;
				client.SetupGL();
				ClientBase.LoadState = LoadState.Done;
				client.OnSetupFinished();
			};

			Resize += e => client.OnResize(e, Size);
			KeyDown += client.OnKeyPress;
			KeyUp += client.OnKeyRelease;
			MouseMove += client.OnMouseMove;
			MouseDown += client.OnMousePress;
			MouseUp += client.OnMouseRelease;
			MouseWheel += client.OnMouseScroll;
			Closing += _ => ClientBase.CloseRequested = true;
			Closing += client.OnClosing;

			client.OnWindowCreation(this);
		}

		public void ToggleFullscreen() {
			WindowState = WindowState == WindowState.Normal ? WindowState.Fullscreen : WindowState.Normal;
			Client.OnFullscreenToggle(WindowState);
		}

		protected override void OnRenderFrame(FrameEventArgs args) {
			Calc(args.Time, ref frameCounter, ref frameTimeCounter, out ClientBase.RawFrameFrequency, ref ClientBase.RawFPS);

			OpenGL4.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Client.Render(args.Time);
			SwapBuffers();
		}

		protected override void OnUpdateFrame(FrameEventArgs args) {
			Calc(args.Time, ref tickCounter, ref tickTimeCounter, out ClientBase.RawTickFrequency, ref ClientBase.RawTPS);

			Client.Tick(args.Time);
		}

		private static void Calc(double time, ref uint counter, ref double timeCounter, out double frequency, ref uint result) {
			frequency = time * 1000;
			counter++;

			if ((timeCounter += time) >= 1) {
				result = counter;
				counter = 0;
				timeCounter -= 1;
			}
		}
	}
}