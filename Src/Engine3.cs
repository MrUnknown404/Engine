using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Engine3.Client.Graphics;
using Engine3.Compatability;
using Engine3.Exceptions;
using Engine3.Utility;
using Engine3.Utility.Versions;
using JetBrains.Annotations;
using NLog;
using OpenTK.Graphics.Vulkan;
using OpenTK.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Silk.NET.Core.Loader;
using Silk.NET.Shaderc;
using StbiSharp;

#if DEBUG
using Engine3.Debug;
#endif

namespace Engine3;

[MustDisposeResource]
public sealed class Engine3 : IDisposable {
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public const string Name = nameof(Engine3);
	public const bool Debug =
#if DEBUG
			true;
#else
			false;
#endif

	[field: MaybeNull]
	public static Engine3 Engine { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(Engine)} too early. {nameof(Initialize)}() must be called first"); private set; }

	public Assembly Assembly { get; } = typeof(Engine3).Assembly;
	public Version4Interweaved Version { get; } = new(0, 0, 0);
	public GraphicsBackend GraphicsBackend { get; }

	[field: MaybeNull]
	public GameClient Game { get => field ?? throw new Engine3Exception($"Attempted to get {nameof(Game)} too early. {nameof(StartGame)}() must be called first"); private set; }

	[field: MaybeNull]
	internal Shaderc Shaderc => field ?? throw new Engine3Exception($"Attempted to get {nameof(Shaderc)} using an unsupported graphics backend ({GraphicsBackend})");

	public bool WasInitialized { get; private set; }
	public bool WasDestroyed { get; private set; }

	public event OnInitializeDelegate? OnInitializeEvent;
	public event OnStartDelegate? OnStartEvent;
	public event OnSetupToolkitDelegate? OnSetupToolkitEvent;
	public event OnShutdownDelegate? OnShutdownEvent;

	public Engine3(GraphicsBackend graphicsBackend) {
		GraphicsBackend = graphicsBackend;

		if (graphicsBackend is GraphicsBackend.Vulkan or GraphicsBackend.OpenGL) { Shaderc = new(Shaderc.CreateDefaultContext(new ShadercSearchPathContainer().GetLibraryNames())); }
	}

	public void Initialize(StartupSettings settings) {
		if (WasInitialized) { throw new Engine3Exception($"Cannot call {nameof(Initialize)} twice"); }

		Engine = this;
		Thread.CurrentThread.Name = settings.MainThreadName;

		LoggerH.Setup(settings.PrintToConsole);
		Logger.Debug("Finished setting up NLog. Hello!");

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			Windows.Setup();
			Logger.Debug($"OS: {RuntimeInformation.OSDescription}");
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			Linux.Setup();
			Logger.Debug($"OS: {RuntimeInformation.OSDescription}");
		} else { Logger.Warn($"Unknown OS: {RuntimeInformation.OSDescription}"); }

		// TODO look into https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/sigterm-signal-handler

		settings.Print();

		Logger.Info("Setting up engine...");
		Logger.Debug($"- Engine Version: {Version}");
		Logger.Debug($"- GLFW Version: {GLFW.GetVersionString()}"); // TODO i have no idea what window manager OpenTK uses. i see GLFW & SDL. but it looks like PAL is just using Win32 API/X11 API directly. help
		Logger.Debug($"- Graphics Backend: {GraphicsBackend}");

		uint spvVersion = 0, spvRevision = 0;
		Shaderc.GetSpvVersion(ref spvVersion, ref spvRevision);
		Logger.Debug($"- SpirV Version: {(spvVersion & 16711680) >> 16}.{(spvVersion & 65280) >> 8} - {spvRevision}");
		Logger.Debug($"- ImGui Version: {ImGuiNet.GetVersion()}");

#if DEBUG
		Logger.Debug("Writing dumps to file outputs...");
		StructLayoutDumper.WriteDumpsToOutput();
#endif

		Stbi.SetFlipVerticallyOnLoad(settings.StbiFlipOnLoad);

		if (GraphicsBackend != GraphicsBackend.Console) {
			Logger.Info("Setting up Toolkit...");

			SetupToolkit(new() {
					ApplicationName = Name,
					Logger = new TkLogger(),
					FeatureFlags = GraphicsBackend switch {
							GraphicsBackend.OpenGL => ToolkitFlags.EnableOpenGL,
							GraphicsBackend.Vulkan => ToolkitFlags.EnableVulkan,
							GraphicsBackend.Console => ToolkitFlags.None,
							_ => throw new ArgumentOutOfRangeException(),
					},
			});

			OnSetupToolkitEvent?.Invoke();
		}

		WasInitialized = true;
		OnInitializeEvent?.Invoke();
	}

	public void StartGame<T>(T game) where T : GameClient {
		if (!WasInitialized) { throw new Engine3Exception($"Attempted to call {nameof(StartGame)}() too early. {nameof(Initialize)}() must be called first"); }

		Game = game;

		OnStartEvent?.Invoke();
		Game.Start();
	}

	private void SetupToolkit(ToolkitOptions toolkitOptions) {
		EventQueue.EventRaised += OnEventRaised;

		Toolkit.Init(toolkitOptions);
	}

	private void Cleanup() {
		Logger.Debug("Cleaning up engine...");

		Shaderc.Dispose();

		Game = null!; // game should (in theory) already be cleaned up
		Engine = null!;

		Logger.Debug("Cleaning OS compatability...");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Windows.Cleanup(); } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { Linux.Cleanup(); }

		if (GraphicsBackend != GraphicsBackend.Console) {
			EventQueue.EventRaised -= OnEventRaised;
			Toolkit.Uninit();
		}

		Logger.Info("Shutting down logger. Goodbye!");
		LogManager.Shutdown();
	}

	public void Dispose() {
		if (WasDestroyed) { throw new Engine3Exception($"Attempted to dispose {nameof(Engine3)} twice"); }
		if (!WasInitialized) { throw new Engine3Exception($"Attempted to call {nameof(Cleanup)}() too early. {nameof(StartGame)}() must be called first"); }

		OnShutdownEvent?.Invoke();

		Cleanup();

		WasDestroyed = true;
	}

	private void OnEventRaised(PalHandle? palHandle, PlatformEventType platformEventType, EventArgs args) { // TODO merge ImGUI events into here
		if (args is WindowEventArgs windowArgs) {
			if (!Game.FindWindow(windowArgs.Window, out Client.Window? window)) {
				Logger.Warn("EventQueue received on an unknown window");
				return;
			}

			switch (args) {
				case CloseEventArgs: window.OnCloseEventArgs(); break;
				case WindowResizeEventArgs resizeArgs: window.OnResizeEventArgs(resizeArgs); break;
				case WindowModeChangeEventArgs modeArgs: window.OnModeChangeEventArgs(modeArgs); break;
				case KeyDownEventArgs downArgs: window.OnKeyDownEventArgs(downArgs); break;
				case KeyUpEventArgs upArgs: window.OnKeyUpEventArgs(upArgs); break;
				case MouseMoveEventArgs moveArgs: window.OnMouseMoveEventArgs(moveArgs); break;
				case MouseButtonDownEventArgs downArgs: window.OnMouseButtonDownEventArgs(downArgs); break;
				case MouseButtonUpEventArgs upArgs: window.OnMouseButtonUpEventArgs(upArgs); break;
				case ScrollEventArgs scrollArgs: window.OnScrollEventArgs(scrollArgs); break;
			}
		} else {
			// ignored atm
		}
	}

	public delegate void OnInitializeDelegate();
	public delegate void OnSetupToolkitDelegate();
	public delegate void OnStartDelegate();
	public delegate void OnShutdownDelegate();

	private class ShadercSearchPathContainer : SearchPathContainer {
		public override string[] Linux => new[] { "libshaderc_shared.so", "libshaderc.so", };
		public override string[] MacOS => new[] { "libshaderc_shared.dylib", };
		public override string[] Android => new[] { "libshaderc_shared.so", };
		public override string[] IOS => new[] { string.Empty, };
		public override string[] Windows64 => new[] { "shaderc_shared.dll", };
		public override string[] Windows86 => new[] { "shaderc_shared.dll", };
	}

	public sealed class StartupSettings {
		[SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public string MainThreadName { get; init; } = "Main";
		public bool StbiFlipOnLoad { get; init; } = true;
		public bool PrintToConsole { get; init; } = true;

		internal void Print() {
			Logger.Trace("Engine Settings");
			Logger.Trace($"- {nameof(MainThreadName)}: {MainThreadName}");
			Logger.Trace($"- {nameof(StbiFlipOnLoad)}: {StbiFlipOnLoad}");
			Logger.Trace($"- {nameof(PrintToConsole)}: {PrintToConsole}");
		}
	}

	public static class VulkanDefaults {
		internal static readonly string[] RequiredValidationLayers = [
#if DEBUG
				"VK_LAYER_KHRONOS_validation", // if OpenTK defines this somewhere, i could not find it
#endif
		];

		internal static readonly string[] RequiredInstanceExtensions = [
				Vk.KhrGetSurfaceCapabilities2ExtensionName,
#if DEBUG
				Vk.ExtDebugUtilsExtensionName,
#endif
		];

		internal static readonly string[] RequiredDeviceExtensions = [ Vk.KhrSwapchainExtensionName, Vk.KhrDynamicRenderingExtensionName, ];
	}

	public static class OpenGLDefaults { }
}