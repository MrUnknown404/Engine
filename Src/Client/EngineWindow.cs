using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenGL4 = OpenTK.Graphics.OpenGL4.GL;

namespace USharpLibs.Engine.Client {
	[PublicAPI]
	public sealed class EngineWindow : GameWindow {
		private GameEngine Client { get; }

		private uint frameCounter, tickCounter;
		private double frameTimeCounter, tickTimeCounter;

		internal EngineWindow(GameEngine client) : base(60,
				new() {
						MinimumSize = new(client.MinWidth, client.MinHeight),
						MaximumSize = client.MaxWidth == 0 || client.MaxHeight == 0 ? null : new(client.MaxWidth, client.MaxHeight),
						Size = new(client.MinWidth, client.MinHeight),
						Title = client.OriginalTitle,
				}) {
			Client = client;

			Resize += client.SetupViewport;
			Resize += _ => GameEngine.CurrentScreen?.OnResize();
			Resize += client.InvokeOnWindowResizeEvent;

			Closing += args => {
				args.Cancel = client.InvokeOnClosingEvent();
				if (!args.Cancel) { GameEngine.CloseRequested = true; }
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

			TextInput += client.InvokeOnTextInputEvent;
		}

		protected override void OnRenderFrame(double time) {
			base.OnRenderFrame(time);
			Calc(time, ref frameCounter, ref frameTimeCounter, out GameEngine.RawFrameFrequency, ref GameEngine.RawFPS);

			OpenGL4.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Client.Render(time);
			SwapBuffers();
		}

		protected override void OnUpdateFrame(double time) {
			base.OnUpdateFrame(time);
			Calc(time, ref tickCounter, ref tickTimeCounter, out GameEngine.RawTickFrequency, ref GameEngine.RawTPS);

			Client.Tick(time);
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