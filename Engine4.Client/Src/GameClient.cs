using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Rendering;
using Engine4.Client.Utility;
using Engine4.IO;
using Engine4.Utility;
using Engine4.Utility.Exceptions;
using Engine4.Utility.Versions;
using NLog;
using OpenTK.Graphics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine4.Client;

// https://docs.vulkan.org/tutorial/latest/Building_a_Simple_Engine/Engine_Architecture/05_rendering_pipeline.html read

public abstract class GameClient : GameCore {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Engine);

	// windowing & graphics
	public bool IsGlfwEnabled { get; private set; }
	public bool IsVulkanEnabled { get; private set; }

	private readonly List<Window> windows = new(); // TODO cleanup. allow removal
	private readonly List<VulkanRenderer> renderers = new(); // TODO cleanup. allow removal

	protected VulkanManager? VulkanManager { get; private set; }
	protected sealed override Action? PollEvents { get; }

	protected GameClient(string name, IPackableVersion version) : base(name, version) {
		if (IsGlfwEnabled) { PollEvents = GLFW.PollEvents; }
	}

	protected sealed override void SetupInternals(StartupSettings coreSettings) {
		if (coreSettings is not ClientStartupSettings clientSettings) { throw new ArgumentException($"{nameof(coreSettings)} needs to be of type {nameof(ClientStartupSettings)}"); }

		base.SetupInternals(coreSettings);

		if (clientSettings.LoadGlfw) {
			Logger.Trace("Loading Glfw...");
			IsGlfwEnabled = true;
			SetupGlfw();
		}

		if (clientSettings.VulkanSettings != null) {
			Logger.Trace("Loading Vulkan...");
			IsVulkanEnabled = true;
			SetupVulkan(clientSettings.VulkanSettings);
		}
	}

	protected sealed override void InternalUpdate() {
		// try close windows
		for (int i = 0; i < windows.Count; i++) {
			Window window = windows[i];

			if (window.GlfwShouldClose()) { window.RequestClose(false); }

			if (window.ShouldClose) {
				Logger.Debug("Found window to close. Closing it...");

				window.Cleanup();
				windows.RemoveAt(i);
				i--;
			}
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

	protected VulkanRenderer CreateRenderer(string debugName, RenderTarget renderTarget, params RenderPass[] renderPasses) {
		if (!IsVulkanEnabled) { throw new Engine4Exception($"Cannot create a {nameof(VulkanRenderer)} when Vulkan is not loaded"); }
		if (VulkanManager == null) { throw new IllegalStateException(); }

		Logger.Debug("Creating renderer...");
		VulkanRenderer renderer = new(debugName, VulkanManager, renderTarget, renderPasses);

		renderers.Add(renderer);
		return renderer;
	}

	private void SetupGlfw() {
		GLFW.SetErrorCallback(GlfwErrorCallback);
		GLFW.Init();

		Logger.Debug($"- Glfw Version: {GLFW.GetVersionString()}");
	}

	private void SetupVulkan(VulkanStartupSettings vulkanSettings) {
		Logger.Trace("Loading Vulkan bindings...");
		VKLoader.Init();

		VulkanManager = new(this, vulkanSettings);

		// TODO vulkan
		// TODO print version
	}

	protected sealed override void InternalCleanup() {
		base.InternalCleanup();

		if (IsVulkanEnabled) {
			Logger.Trace("Cleaning up Vulkan");

			if (VulkanManager == null) { throw new IllegalStateException(); }

			foreach (VulkanRenderer renderer in renderers) { renderer.Cleanup(); }

			if (IsGlfwEnabled) {
				Logger.Trace($"Cleaning up {windows.Count} windows");
				foreach (Window window in windows) { window.Cleanup(); }
			}

			VulkanManager.Cleanup();
		}

		if (IsGlfwEnabled) {
			Logger.Trace("Cleaning up Glfw");

			GLFW.Terminate();
			GLFW.SetErrorCallback(null);
		}
	}

	private static readonly Logger GlfwLogger = LoggerH.GetLogger(LogSource.Glfw);
	private static void GlfwErrorCallback(ErrorCode error, string description) => GlfwLogger.Error($"[{error}] {description}");
}