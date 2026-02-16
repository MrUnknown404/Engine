using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.OpenGL;
using Engine3.Exceptions;
using Engine3.Utility;
using Engine3.Utility.Versions;
using NLog;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Core.Loader;
using Silk.NET.Shaderc;
using StbiSharp;
using MouseMoveEventArgs = OpenTK.Platform.MouseMoveEventArgs;
using Window = Engine3.Client.Window;

#if DEBUG
using Engine3.Debug;
#endif

namespace Engine3 {
	// TODO call timeBeginPeriod/timeEndPeriod on windows https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-sleep

	public abstract class GameClient {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		[field: MaybeNull] public Assembly Assembly { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(GameClient)} Assembly too early. Must call {nameof(GameClient)}#{nameof(Start)} first"); private set; }

		internal Shaderc Shaderc { get; } = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames()));

		public string Name { get; }
		public IPackableVersion Version { get; }
		public EngineGraphicsBackend GraphicsBackend { get; }

		private readonly List<Window> windows = new();
		private readonly List<Renderer> renderers = new();

		public ushort TargetUps { get; init; } = 60;
		public ushort TargetFps { get; init; }
		public byte MaxFrameSkip { get; init; } = 5;

		public ulong UpdateIndex { get; private set; }
		public ulong FrameIndex { get; private set; }

		public PerformanceMonitor PerformanceMonitor { get; init; } = new();

		public bool WasGraphicsSetup { get; private set; }

		private readonly Queue<Window> windowCloseQueue = new();
		private readonly Queue<Renderer> renderersCloseQueue = new();

		private bool wasSetup;
		private bool shouldRunGameLoop = true;
		private bool requestShutdown;

		public event Action? OnSetupFinishedEvent;
		public event Action? OnSetupToolkitEvent;
		public event Action? OnShutdownEvent;

		protected GameClient(string name, IPackableVersion version, EngineGraphicsBackend graphicsBackend) {
			Name = name;
			Version = version;
			GraphicsBackend = graphicsBackend;

			if (GraphicsBackend is OpenGLGraphicsBackend glBackend) {
				OpenGLGraphicsApiHints graphicsApiHints = glBackend.GraphicsApiHints as OpenGLGraphicsApiHints ?? throw new NullReferenceException();
				graphicsApiHints.Version = new(4, 6);
				graphicsApiHints.Profile = OpenGLProfile.Core;
#if DEBUG
				graphicsApiHints.DebugFlag = true;
#endif
			}
		}

		public void Start<T>(T gameClient, StartupSettings settings) where T : GameClient {
			if (wasSetup) { throw new Engine3Exception("Attempted to call #Start twice"); }

			Thread.CurrentThread.Name = settings.MainThreadName;

			LoggerH.Setup(GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console);
			Logger.Debug("Finished setting up NLog");

			Assembly = Assembly.GetCallingAssembly();
			Logger.Debug("Got instance assembly");

			Engine3.GameInstance = gameClient;

			if (GraphicsBackend is { GraphicsBackend: not Client.Graphics.GraphicsBackend.Console, GraphicsApiHints: null, }) { throw new Engine3Exception($"GraphicsApiHints cannot be null with GraphicsApi: {GraphicsBackend}"); }

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine Version: {Engine3.Version}");
			Logger.Debug($"- Game Version: {Version}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW, & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
			Logger.Debug($"- ImGui Version: {ImGuiNet.GetVersion()}");
			Logger.Debug($"- Graphics Api: {GraphicsBackend.GraphicsBackend}");

			uint spvVersion = 0, spvRevision = 0;
			Shaderc.GetSpvVersion(ref spvVersion, ref spvRevision);
			Logger.Debug($"- SpirV Version: {(spvVersion & 16711680) >> 16}.{(spvVersion & 65280) >> 8} - {spvRevision}");

			SetupEngine(settings);

			wasSetup = true;

			Logger.Debug("Setup finished. Invoking events then entering loop");
			OnSetupFinishedEvent?.Invoke();

			GameLoop();

			Logger.Info("GameLoop exited");
			OnShutdownEvent?.Invoke();

			Logger.Debug("Cleaning up everything...");
			CleanupEverything();
			Environment.Exit(0);
		}

		protected abstract void Update();
		protected abstract void Cleanup();

		private void SetupEngine(StartupSettings settings) {
			settings.Print();

#if DEBUG
			Logger.Debug("Writing dumps to file outputs...");
			StructLayoutDumper.WriteDumpsToOutput();
#endif

			Stbi.SetFlipVerticallyOnLoad(settings.StbiFlipOnLoad);

			Logger.Info("Setting up Toolkit...");
			SetupToolkit(new() {
					ApplicationName = Name,
					Logger = new TkLogger(),
					FeatureFlags = GraphicsBackend.GraphicsBackend switch {
							Client.Graphics.GraphicsBackend.OpenGL => ToolkitFlags.EnableOpenGL,
							Client.Graphics.GraphicsBackend.Vulkan => ToolkitFlags.EnableVulkan,
							Client.Graphics.GraphicsBackend.Console => ToolkitFlags.None,
							_ => throw new ArgumentOutOfRangeException(),
					},
			}, GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console);

			OnSetupToolkitEvent?.Invoke();

			Logger.Debug($"Setting up {Enum.GetName(GraphicsBackend.GraphicsBackend)}...");
			GraphicsBackend.Setup(this);
			WasGraphicsSetup = true;
		}

		private void EngineUpdate() { }

		private void GameLoop() {
			const long TicksPerSecond = 1000000000; // Stopwatch.Frequency;

			long updateTicksToWait = TicksPerSecond / TargetUps;
			long frameTicksToWait = TargetFps == 0 ? 0 : TicksPerSecond / TargetFps;

			long currentTime = Stopwatch.GetTimestamp();
			long updateAccumulator = 0;
			long lastFrameTime = 0;

			while (shouldRunGameLoop) {
				if (GraphicsBackend.GraphicsBackend != Client.Graphics.GraphicsBackend.Console) { Toolkit.Window.ProcessEvents(false); }
				if (requestShutdown) { shouldRunGameLoop = false; } // TODO check more?

				if (!shouldRunGameLoop) { break; } // Early exit

				long time = PerformanceMonitor.GetTimeDifference(ref currentTime);
				Update(time);

				// console end. VK/GL graphics below // TODO impl console rendering
				if (GraphicsBackend.GraphicsBackend == Client.Graphics.GraphicsBackend.Console) { continue; }

				TryCloseWindows();
				TryDestroyRenderers();

				Render(time);
			}

			return;

			void Update(long time) {
				updateAccumulator += time;
				PerformanceMonitor.AddUpdateAccumulator(time);

				int frameSkip = 0;
				while (updateAccumulator >= updateTicksToWait && frameSkip < MaxFrameSkip) {
					PerformanceMonitor.StartTimingUpdate();
					EngineUpdate();
					this.Update();
					PerformanceMonitor.StopTimingUpdate();

					updateAccumulator -= updateTicksToWait;
					UpdateIndex++;
					frameSkip++;
					PerformanceMonitor.AddUpdate();

					PerformanceMonitor.CheckUpdateTime();

					if (frameSkip >= MaxFrameSkip) { Logger.Warn($"FrameSkip hit max. ({MaxFrameSkip})"); }
				}
			}

			void Render(long time) {
				if (TargetFps != 0) {
					while (Stopwatch.GetTimestamp() < lastFrameTime + frameTicksToWait) { Thread.Sleep(0); }
					lastFrameTime = Stopwatch.GetTimestamp();
				}

				PerformanceMonitor.AddFrameAccumulator(time);

				float delta = 1 - (float)(updateTicksToWait - updateAccumulator) / updateTicksToWait;

				PerformanceMonitor.StartTimingFrame();
				foreach (Renderer renderer in renderers.Where(static pipeline => pipeline.CanRender)) { renderer.Render(delta); } // TODO check if window is minimized
				PerformanceMonitor.StopTimingFrame();

				FrameIndex++;
				PerformanceMonitor.AddFrame();

				PerformanceMonitor.CheckFrameTime();
			}

			void TryCloseWindows() {
				foreach (Window window2 in windows.Where(static window => window.ShouldClose)) {
					Logger.Debug("Found window to destroy...");
					windowCloseQueue.Enqueue(window2);
				}

				while (windowCloseQueue.TryDequeue(out Window? window)) { RemoveWindow(window); }
			}

			void TryDestroyRenderers() {
				foreach (Renderer renderer in renderers.Where(static renderer => renderer.ShouldDestroy)) {
					Logger.Debug($"Found {nameof(Renderer)} to destroy...");
					renderersCloseQueue.Enqueue(renderer);
				}

				while (renderersCloseQueue.TryDequeue(out Renderer? renderer)) { RemoveRenderer(renderer); }
			}

			void RemoveWindow<T>(T window) where T : Window {
				if (windows.Remove(window)) {
					foreach (Renderer renderer in renderers.Where(renderer => renderer.IsSameWindow(window))) { RemoveRenderer(renderer); }

					Logger.Debug($"Destroying {nameof(Window)}...");
					window.Destroy();
				} else { Logger.Error($"Could not find to be destroyed {nameof(Window)} in {nameof(GameClient)}'s {nameof(Window)} list"); }
			}

			void RemoveRenderer<T>(T renderer) where T : Renderer {
				if (renderers.Remove(renderer)) {
					Logger.Debug($"Destroying {nameof(Renderer)}...");
					renderer.Destroy();
				} else { Logger.Error($"Could not find to be destroyed {nameof(Renderer)} in {nameof(GameClient)}'s {nameof(Renderer)} list"); }
			}
		}

		private void SetupToolkit(ToolkitOptions toolkitOptions, bool isConsole) {
			if (!isConsole) { EventQueue.EventRaised += OnEventQueueOnEventRaised; }

			Toolkit.Init(toolkitOptions);

			return;

			// TODO hate that i have to do this. let me have events per window. see if i can find a way to do that
			void OnEventQueueOnEventRaised(PalHandle? palHandle, PlatformEventType platformEventType, EventArgs args) {
				switch (args) {
					case CloseEventArgs closeArgs: {
						if (!FindWindow(closeArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to close an unknown window");
							break;
						}

						window.TryCloseWindow();
						break;
					}
					case WindowResizeEventArgs resizeArgs: {
						if (!FindWindow(resizeArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to resize an unknown window");
							break;
						}

						window.WasResized = true;
						window.InvokeOnResize((uint)resizeArgs.NewClientSize.X, (uint)resizeArgs.NewClientSize.Y);
						break;
					}
					case KeyDownEventArgs downArgs: {
						if (!FindWindow(downArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to provide input to an unknown window");
							break;
						}

						window.KeyManager.SetKey(downArgs.Key, true);
						break;
					}
					case KeyUpEventArgs upArgs: {
						if (!FindWindow(upArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to provide input to an unknown window");
							break;
						}

						window.KeyManager.SetKey(upArgs.Key, false);
						break;
					}
					case MouseMoveEventArgs moveArgs: {
						if (!FindWindow(moveArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to provide input to an unknown window");
							break;
						}

						window.MouseManager.Position = new(moveArgs.ClientPosition.X, moveArgs.ClientPosition.Y);
						break;
					}
					case MouseButtonDownEventArgs downArgs: {
						if (!FindWindow(downArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to provide input to an unknown window");
							break;
						}

						window.MouseManager.SetButton(downArgs.Button, true);
						break;
					}
					case MouseButtonUpEventArgs upArgs: {
						if (!FindWindow(upArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to provide input to an unknown window");
							break;
						}

						window.MouseManager.SetButton(upArgs.Button, false);
						break;
					}
					case ScrollEventArgs scrollArgs: {
						if (!FindWindow(scrollArgs.Window, out Window? window)) {
							Logger.Warn("Attempted to provide input to an unknown window");
							break;
						}

						window.MouseManager.ScrollDelta = scrollArgs.Delta.Y;
						break;
					}
				}
			}

			bool FindWindow(WindowHandle windowHandle, [NotNullWhen(true)] out Window? window) => (window = windows.Find(w => w.WindowHandle == windowHandle)) != null;
		}

		public void AddWindow<T>(T window) where T : Window => windows.Add(window);
		public void AddRenderer<T>(T renderer) where T : Renderer => renderers.Add(renderer);

		public void Shutdown() {
			Logger.Debug("Shutdown called");
			requestShutdown = true;
		}

		private void CleanupEverything() {
			Logger.Debug("Cleaning up engine...");
			CleanupEngine();

			Logger.Debug("Cleaning up instance...");
			Cleanup();

			Logger.Debug($"Cleaning up {renderers.Count} {nameof(Renderer)}s...");
			foreach (Renderer renderer in renderers) { renderer.Destroy(); }

			Logger.Debug($"Cleaning up {windows.Count} {nameof(Window)}s...");
			foreach (Window window in windows) { window.Destroy(); }

			Logger.Debug("Cleaning up ImGui...");
			ImGuiH.Cleanup();

			Logger.Debug("Cleaning up graphics...");
			GraphicsBackend.Cleanup();

			Logger.Info("Goodbye!");
			LogManager.Shutdown();
		}

		private void CleanupEngine() => Shaderc.Dispose();

		public class StartupSettings {
			[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
			private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

			public string MainThreadName { get; init; } = "Main";
			public bool StbiFlipOnLoad { get; init; } = true;

			internal void Print() {
				Logger.Trace("Engine Startup Settings");
				Logger.Trace($"- {nameof(MainThreadName)}: {MainThreadName}");
				Logger.Trace($"- {nameof(StbiFlipOnLoad)}: {StbiFlipOnLoad}");
			}
		}

		private class ShadercSearchPathContainer : SearchPathContainer {
			public override string[] Linux => new[] { "libshaderc_shared.so", "libshaderc.so", };
			public override string[] MacOS => new[] { "libshaderc_shared.dylib", };
			public override string[] Android => new[] { "libshaderc_shared.so", };
			public override string[] IOS => new[] { string.Empty, };
			public override string[] Windows64 => new[] { "shaderc_shared.dll", };
			public override string[] Windows86 => new[] { "shaderc_shared.dll", };
		}
	}
}