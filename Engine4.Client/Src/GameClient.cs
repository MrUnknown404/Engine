using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Utility;
using Engine4.IO;
using Engine4.Utility;
using Engine4.Utility.Exceptions;
using Engine4.Utility.Math;
using Engine4.Utility.Versions;
using NLog;
using OpenTK.Graphics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OperatingSystem = Engine4.Utility.Compatability.OperatingSystem;

namespace Engine4.Client;

// https://docs.vulkan.org/tutorial/latest/Building_a_Simple_Engine/Engine_Architecture/05_rendering_pipeline.html read

public abstract class GameClient : GameCore {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	// windowing & graphics
	public bool IsGlfwEnabled { get; private set; } // note: not set until SetupInternals()
	public bool IsVulkanEnabled { get; private set; }

	private readonly List<Window> windows = new(); // TODO window/renderer should impl HashCode
	private readonly List<VulkanRenderer> renderers = new(); // TODO allow removal
	private readonly Dictionary<Window, VulkanRenderer> windowToRenderer = new();

	protected VulkanManager? VulkanManager { get; private set; }

	protected bool AnyWindowsExist => windows.Count != 0;

	protected GameClient(string name, IPackableVersion version) : base(name, version) { }

	protected sealed override void SetupInternals(StartupSettings coreSettings) {
		if (coreSettings is not ClientStartupSettings clientSettings) { throw new ArgumentException($"{nameof(coreSettings)} needs to be of type {nameof(ClientStartupSettings)}"); }

		base.SetupInternals(coreSettings);

		if (clientSettings.LoadGlfw) {
			Logger.Debug("Loading Glfw...");
			IsGlfwEnabled = true;

			SetupGlfw();
		}

		if (clientSettings.VulkanSettings != null) {
			Logger.Debug("Loading Vulkan...");
			IsVulkanEnabled = true;

			SetupVulkan(clientSettings.VulkanSettings);
		}
	}

	protected sealed override void InternalUpdate() {
		// try close windows
		for (int i = 0; i < windows.Count; i++) {
			Window window = windows[i];

			if (window.GlfwShouldClose()) { window.RequestClose(false); }
			if (!window.ShouldClose) { continue; }

			Logger.Debug("Found window to close. Closing it...");

			if (windowToRenderer.TryGetValue(window, out VulkanRenderer? renderer)) {
				Logger.Trace("- Window has a renderer. Cleaning that first...");

				renderer.Cleanup(); // calls Vk.DeviceWaitIdle()
				renderers.Remove(renderer);
			}

			window.Cleanup();
			windows.RemoveAt(i);
			i--;
		}

		base.InternalUpdate();
	}

	protected sealed override void InternalRender(float delta) {
		base.InternalRender(delta); //
	}

	protected sealed override void Render(float delta) {
		foreach (VulkanRenderer renderer in renderers) { renderer.InternalRender(delta); } // TODO time individual renderers
	}

	protected Window CreateWindow(string title, ushort width, ushort height) {
		if (!IsGlfwEnabled) { throw new Engine4Exception($"Cannot create a {nameof(Window)} when Glfw is not loaded"); }
		if (VulkanManager == null) { throw new IllegalStateException(); }

		Logger.Debug("Creating window...");
		Window window = new(title, width, height);

		windows.Add(window);
		return window;
	}

	protected VulkanRenderer CreateRenderer(string debugName, Window window, Color4 clearColor) {
		if (!IsVulkanEnabled) { throw new Engine4Exception($"Cannot create a {nameof(VulkanRenderer)} when Vulkan is not loaded"); }
		if (VulkanManager == null) { throw new IllegalStateException(); }

		Logger.Debug("Creating renderer...");
		VulkanRenderer renderer = new(debugName, VulkanManager, window, clearColor);

		renderers.Add(renderer);
		windowToRenderer.Add(window, renderer);

		return renderer;
	}

	private void SetupGlfw() {
		switch (Engine4.OperatingSystem) {
			case OperatingSystem.Linux: GLFW.InitHint(InitHintPlatform.Platform, Platform.Wayland); break;
			case OperatingSystem.Windows: break; // nothing atm
			default: throw new ArgumentOutOfRangeException();
		}

		GLFW.SetErrorCallback(GlfwErrorCallback);
		GLFW.Init();

		// prefer wayland over x11. Renderdoc uses x11 (pain).
		// if GLFW doesn't have any instance extensions then try again with x11.
		// this is basically done to check if we're using Renderdoc.
		// if there's a better way of doing this lemme know.
		if (GLFW.GetRequiredInstanceExtensions().Length == 0) {
			Logger.Warn("Failed to find GLFW instance extensions. Attempting to try again with X11.");
			GLFW.Terminate();

			GLFW.InitHint(InitHintPlatform.Platform, Platform.X11);
			GLFW.Init();

			if (GLFW.GetRequiredInstanceExtensions().Length == 0) { throw new Engine4Exception("Failed to setup GLFW"); }
		}

		PollEvents = GLFW.PollEvents;

		Logger.Debug($"GLFW successfully set up. Version: {GLFW.GetVersionString()}, Platform: {GLFW.GetPlatform()}");
	}

	private void SetupVulkan(VulkanStartupSettings vulkanSettings) {
		Logger.Trace("Loading Vulkan bindings...");
		VKLoader.Init();

		// get vulkan api version
		VkH.GetApiVersion(out uint apiVersion, out _, out byte major, out ushort minor, out ushort patch);
		Logger.Debug($"- Version: {major}.{minor}.{patch} ({apiVersion})");

		VulkanManager = new(this, vulkanSettings);
		// TODO print version
	}

	protected sealed override void InternalCleanup() {
		base.InternalCleanup();

		if (IsVulkanEnabled) {
			Logger.Trace("Cleaning up Vulkan...");

			if (VulkanManager == null) { throw new IllegalStateException(); } // if vulkan is enabled these should not be null

			if (IsGlfwEnabled) {
				Logger.Trace($"- Cleaning up {renderers.Count} renderers...");
				foreach (VulkanRenderer renderer in renderers) {
					renderer.Cleanup(); // calls Vk.DeviceWaitIdle()
				}
			}

			VulkanManager.Cleanup();
		}

		if (IsGlfwEnabled) {
			Logger.Trace("Cleaning up Glfw...");

			Logger.Trace($"- Cleaning up {windows.Count} windows...");
			foreach (Window window in windows) { window.Cleanup(); }

			GLFW.Terminate();
			GLFW.SetErrorCallback(null);
		}
	}

	private static readonly Logger GlfwLogger = LoggerH.GetLogger(LogSource.Glfw);
	private static void GlfwErrorCallback(ErrorCode error, string description) => GlfwLogger.Error($"[{error}] {description}");
}