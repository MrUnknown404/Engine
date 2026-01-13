using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine3.Debug;
using Engine3.Exceptions;
using Engine3.Utils;
using NLog;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using shaderc;
using ZLinq;
using GraphicsApi = Engine3.Graphics.GraphicsApi;
using Window = Engine3.Graphics.Window;
using SpirVCompiler = shaderc.Compiler;

namespace Engine3 {
	public static partial class Engine3 { // TODO remove this class and merge into GameClient
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public const string Name = nameof(Engine3);

		public const bool Debug =
#if DEBUG
				true;
#else
				false;
#endif

		// ReSharper disable once InconsistentNaming // backing field. name is taken
		private static readonly Lazy<Assembly> engineAssembly = new(static () => Assembly.GetAssembly(typeof(Engine3)) ?? throw new NullReferenceException("Failed to get Engine3 Assembly"));

		public static Version4 EngineVersion { get; } = new(0, 0, 0);
		public static Assembly EngineAssembly => engineAssembly.Value;
		public static GameClient? GameInstance { get; private set; }

		public static GraphicsApi GraphicsApi { get; private set; } = GraphicsApi.Console;
		public static GraphicsApiHints? GraphicsApiHints { get; private set; }

		public static ulong UpdateFrameCount { get; private set; }
		public static uint UpdatesPerSecond { get; private set; }
		public static float UpdateFrameTime { get; private set; }

		private static bool shouldRunGameLoop = true;
		private static bool requestShutdown;

		public static event Action? PreEngineSetupEvent;
		public static event Action? PostEngineSetupEvent;
		public static event Action? OnSetupFinishedEvent;
		public static event Action? OnSetupToolkitEvent;
		public static event Action? OnShutdownEvent;

		public static void Start<T>(StartupSettings settings) where T : GameClient, new() {
			if (GameInstance != null) {
				Logger.Error("Attempted to call #Start twice");
				return;
			}

			Thread.CurrentThread.Name = settings.MainThreadName;

			LoggerH.Setup();
			Logger.Debug("Finished setting up NLog");

			GraphicsApi = settings.GraphicsApi;
			GraphicsApiHints = settings switch {
					OpenGLSettings glSettings => glSettings.GraphicsApiHints,
					VulkanSettings vkSettings => vkSettings.GraphicsApiHints,
					_ => null,
			};

			if (GraphicsApi != GraphicsApi.Console && GraphicsApiHints == null) { throw new Engine3Exception($"GraphicsApiHints cannot be null with GraphicsApi: {GraphicsApi}"); }

			Logger.Info("Setting up engine...");
			Logger.Debug($"- Engine Version: {EngineVersion}");
			Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW, & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
			Logger.Debug($"- Graphics Api: {GraphicsApi}");
			SpirVCompiler.GetSpvVersion(out SpirVVersion version, out uint revision);
			Logger.Debug($"- SpirV Version: {version.Major}.{version.Minor} - {revision}");

			PreEngineSetupEvent?.Invoke();
			SetupEngine();
			PostEngineSetupEvent?.Invoke();

			Logger.Info("Creating game instance...");

			if (GraphicsApi == GraphicsApi.Vulkan) {
				VulkanSettings vkSettings = settings as VulkanSettings ?? throw new UnreachableException();

				GameInstance = new T {
						Assembly = Assembly.GetAssembly(typeof(T)) ?? throw new NullReferenceException($"Failed to get assembly for '{typeof(T).Name}'"),
						Name = settings.GameName,
#if DEBUG
						RequiredValidationLayers = vkSettings.RequiredValidationLayers,
#endif
						RequiredInstanceExtensions = vkSettings.RequiredInstanceExtensions,
						RequiredDeviceExtensions = vkSettings.RequiredDeviceExtensions,
						EnabledDebugMessageSeverities = vkSettings.EnabledDebugMessageSeverities,
						EnabledDebugMessageTypes = vkSettings.EnabledDebugMessageTypes,
						MaxFramesInFlight = vkSettings.MaxFramesInFlight,
				};
			} else {
				GameInstance = new T { Assembly = Assembly.GetAssembly(typeof(T)) ?? throw new NullReferenceException($"Failed to get assembly for '{typeof(T).Name}'"), Name = settings.GameName, }; //
			}

			Logger.Debug($"- Game Version: {GameInstance.Version}");

			GameInstance.Setup();

#if DEBUG
			Logger.Debug("Writing dumps to file outputs...");
			StructLayoutDumper.WriteDumpsToOutput();
#endif

			if (GraphicsApi != GraphicsApi.Console) {
				Logger.Info("Setting up Toolkit...");
				SetupToolkit(settings switch {
						OpenGLSettings glSettings => glSettings.ToolkitOptions,
						VulkanSettings vkSettings => vkSettings.ToolkitOptions,
						_ => throw new UnreachableException(),
				});

				OnSetupToolkitEvent?.Invoke();
			}

			if (GraphicsApi == GraphicsApi.Vulkan) {
				Logger.Info("Setting up Vulkan...");
				SetupVulkan(GameInstance, EngineVersion);
			}

			Logger.Debug("Setup finished. Invoking events then entering loop");
			OnSetupFinishedEvent?.Invoke();

			GameLoop();

			Logger.Debug("GameLoop exited");
			OnShutdownEvent?.Invoke();

			Cleanup();
			Environment.Exit(0);
		}

		private static void SetupEngine() { }

		private static void SetupToolkit(ToolkitOptions toolkitOptions) {
			EventQueue.EventRaised += static (_, _, args) => {
				if (GameInstance is not { } gameInstance) { return; }

				switch (args) {
					case CloseEventArgs closeArgs: {
						if (gameInstance.Windows.Find(w => w.WindowHandle == closeArgs.Window) is { } window) {
							window.TryCloseWindow();
							return;
						}

						Logger.Warn("Attempted to close an unknown window");
						break;
					}
					case WindowResizeEventArgs resizeArgs: {
						if (gameInstance.Windows.Find(w => w.WindowHandle == resizeArgs.Window) is { } window) {
							window.WasResized = true;
							return;
						}

						Logger.Warn("Attempted to resize an unknown window");
						break;
					}
				}
			};

			Toolkit.Init(toolkitOptions);
		}

		private static void GameLoop() {
			Stopwatch stopwatch = new(); // TODO remove
			stopwatch.Start();
			uint fpsCounter = 0;

			if (GameInstance is not { } gameInstance) { throw new UnreachableException(); }

			while (shouldRunGameLoop) {
				if (GraphicsApi != GraphicsApi.Console) { Toolkit.Window.ProcessEvents(false); }
				if (requestShutdown) { shouldRunGameLoop = false; } // TODO check more?
				if (!shouldRunGameLoop) { break; } // Early exit

				Update();
				gameInstance.Update();
				UpdateFrameCount++;

				if (GraphicsApi == GraphicsApi.Console) { continue; }

				float delta = 0; // TODO impl

				foreach (Window window in gameInstance.Windows.Where(static w => w is { WasDestroyed: false, })) {
					if (window.ShouldClose) {
						window.DestroyWindow();
						continue;
					}

					if (window.Renderer is { } renderer) { renderer.DrawFrame(delta); }
				}

				fpsCounter++;
				if (stopwatch.Elapsed.TotalSeconds >= 1) {
					Logger.Debug($"Fps: {fpsCounter}");
					stopwatch.Restart();
					fpsCounter = 0;
				}
			}
		}

		private static void Update() { }

		public static void Shutdown() {
			Logger.Debug("Shutdown called");
			requestShutdown = true;
		}

		private static void Cleanup() {
			if (GameInstance is { } gameInstance) {
				gameInstance.Cleanup();

				foreach (Window window in gameInstance.Windows.Where(static w => !w.WasDestroyed)) { window.DestroyWindow(); }
			}

			if (GraphicsApi == GraphicsApi.Vulkan) { CleanupVulkan(); }

			Logger.Debug("Goodbye!");
			LogManager.Shutdown();
		}

		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		public abstract class StartupSettings {
			public required string GameName { get; init; }
			public string MainThreadName { get; init; } = "Main";
			public abstract GraphicsApi GraphicsApi { get; }

			[SetsRequiredMembers] protected StartupSettings(string gameName) => GameName = gameName;
		}
	}
}