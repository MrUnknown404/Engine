using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public sealed class EngineWindow : GameWindow {
		private GameEngine Client { get; }

		private uint frameCounter, tickCounter;
		private double frameTimeCounter, tickTimeCounter;

		public EngineWindow(GameEngine client) : base(GameWindowSettings.Default,
				new NativeWindowSettings {
						MinimumSize = new(client.MinWidth, client.MinHeight),
						MaximumSize = client.MaxWidth == 0 || client.MaxHeight == 0 ? null : new(client.MaxWidth, client.MaxHeight),
						Size = new(client.MinWidth, client.MinHeight),
						Title = client.OriginalTitle,
				}) {
			Client = client;
			UpdateFrequency = 60;

			Load += () => {
				GameEngine.LoadState = LoadState.CreateGL;
				GameEngine.CreateGL();
				GameEngine.LoadState = LoadState.SetupGL;
				client.InvokeOnSetupLoadingScreenEvent();
				client.SetupGL();
				GameEngine.LoadState = LoadState.Done;
				client.InvokeOnSetupFinishEvent();
			};

			Resize += e => OpenGL4.Viewport(0, 0, e.Width, e.Height);
			Closing += _ => {
				GameEngine.CloseRequested = true;
				client.InvokeOnClosingEvent();
			};

			KeyDown += client.InvokeOnKeyPressEvent;
			KeyUp += client.InvokeOnKeyReleaseEvent;
			MouseWheel += client.InvokeOnMouseScrollEvent;
			MouseMove += client.InvokeOnMouseMoveEvent;

			MouseDown += e => {
				if (!client.CheckScreenMousePress(e) || !client.ShouldScreenCheckCancelMouseEvent) { client.InvokeOnMousePressEvent(e); }
			};

			MouseUp += e => {
				if (!client.CheckScreenMouseRelease(e) || !client.ShouldScreenCheckCancelMouseEvent) { client.InvokeOnMouseReleaseEvent(e); }
			};

			client.InvokeWindowCreationEvent(this);
		}

		protected override void OnRenderFrame(FrameEventArgs args) {
			Calc(args.Time, ref frameCounter, ref frameTimeCounter, out GameEngine.RawFrameFrequency, ref GameEngine.RawFPS);

			OpenGL4.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Client.Render(args.Time);
			SwapBuffers();
		}

		protected override void OnUpdateFrame(FrameEventArgs args) {
			Calc(args.Time, ref tickCounter, ref tickTimeCounter, out GameEngine.RawTickFrequency, ref GameEngine.RawTPS);

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