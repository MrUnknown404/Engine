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
				client.SetupLoadingScreen();
				client.SetupGL();
				GameEngine.LoadState = LoadState.Done;
				client.OnSetupFinished();
			};

			Resize += e => client.OnResize(e, Size);
			Closing += _ => GameEngine.CloseRequested = true;
			Closing += client.OnClosing;

			KeyDown += client.OnKeyPress;
			KeyUp += client.OnKeyRelease;
			MouseWheel += client.OnMouseScroll;

			MouseMove += args => {
				GameEngine.MouseX = (ushort)MathH.Floor(args.X);
				GameEngine.MouseY = (ushort)MathH.Floor(args.Y);
				GameEngine.CurrentScreen?.CheckForHover(GameEngine.MouseX, GameEngine.MouseY);
			};

			MouseMove += client.OnMouseMove;

			MouseDown += args => {
				if (args.Action != InputAction.Repeat) {
					if (args.Button is MouseButton.Left or MouseButton.Right) { GameEngine.CurrentScreen?.CheckForFocus(GameEngine.MouseX, GameEngine.MouseY); }
					GameEngine.CurrentScreen?.CheckForPress(args.Button, GameEngine.MouseX, GameEngine.MouseY);
				}
			};

			MouseDown += client.OnMousePress;

			MouseUp += args => {
				if (args.Button is MouseButton.Left or MouseButton.Right && args.Action != InputAction.Repeat) { GameEngine.CurrentScreen?.CheckForRelease(args.Button, GameEngine.MouseX, GameEngine.MouseY); }
			};

			MouseUp += client.OnMouseRelease;

			client.OnWindowCreation(this);
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