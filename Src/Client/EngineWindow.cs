using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using USharpLibs.Common.Utils;
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
				ClientBase.LoadState = LoadState.CreateGL;
                ClientBase.CreateGL();
				ClientBase.LoadState = LoadState.SetupGL;
				client.SetupLoadingScreen();
				client.SetupGL();
				ClientBase.LoadState = LoadState.Done;
				client.OnSetupFinished();
			};

			Resize += e => client.OnResize(e, Size);
			Closing += _ => ClientBase.CloseRequested = true;
			Closing += client.OnClosing;

			KeyDown += client.OnKeyPress;
			KeyUp += client.OnKeyRelease;
			MouseWheel += client.OnMouseScroll;

			MouseMove += client.OnMouseMove;
			MouseMove += args => {
				ClientBase.MouseX = (ushort)MathH.Floor(args.X);
				ClientBase.MouseY = (ushort)MathH.Floor(args.Y);
				ClientBase.CurrentScreen?.CheckForHover(ClientBase.MouseX, ClientBase.MouseY);
			};

			MouseDown += client.OnMousePress;
			MouseDown += args => {
				if (args.Action != InputAction.Repeat) {
					if (args.Button is MouseButton.Left or MouseButton.Right) { ClientBase.CurrentScreen?.CheckForFocus(ClientBase.MouseX, ClientBase.MouseY); }
					ClientBase.CurrentScreen?.CheckForPress(args.Button, ClientBase.MouseX, ClientBase.MouseY);
				}
			};

			MouseUp += client.OnMouseRelease;
			MouseUp += args => {
				if (args.Button is MouseButton.Left or MouseButton.Right && args.Action != InputAction.Repeat) { ClientBase.CurrentScreen?.CheckForRelease(args.Button, ClientBase.MouseX, ClientBase.MouseY); }
			};

			client.OnWindowCreation(this);
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