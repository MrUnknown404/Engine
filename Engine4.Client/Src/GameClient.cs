using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Rendering;
using Engine4.IO;
using Engine4.Utility.Exceptions;
using Engine4.Utility.Versions;
using NLog;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine4.Client;

// https://docs.vulkan.org/tutorial/latest/Building_a_Simple_Engine/Engine_Architecture/05_rendering_pipeline.html read

public abstract class GameClient : GameCore {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Game);

	public bool IsGlfwEnabled { get; private set; }
	public bool IsVulkanEnabled { get; private set; }

	protected sealed override Action? PollEvents { get; }

	// windowing & graphics
	private readonly List<Window> windows = new(); // TODO cleanup. allow removal
	private readonly List<VulkanRenderer> renderers = new(); // TODO cleanup. allow removal

	private VulkanResourceManager? vulkanResourceManager;

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

		if (clientSettings.LoadVulkan) {
			Logger.Trace("Loading Vulkan...");
			IsVulkanEnabled = true;
			SetupVulkan();
		}
	}

	protected sealed override void InternalUpdate() {
		base.InternalUpdate(); //
	}

	protected sealed override void InternalRender(float delta) {
		base.InternalRender(delta); //
	}

	protected sealed override void Render(float delta) {
		foreach (VulkanRenderer renderer in renderers) { renderer.InternalRender(delta); }
	}

	protected Window CreateWindow(string title, ushort width, ushort height) {
		if (!IsGlfwEnabled) { throw new Engine4Exception($"Cannot create a {nameof(Window)} when Glfw is not loaded"); }

		Logger.Debug("Creating window...");
		Window window = new(title, width, height);

		windows.Add(window);
		return window;
	}

	protected VulkanRenderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		if (!IsVulkanEnabled) { throw new Engine4Exception($"Cannot create a {nameof(VulkanRenderer)} when Vulkan is not loaded"); }
		if (vulkanResourceManager == null) { throw new IllegalStateException(); }

		Logger.Debug("Creating renderer...");
		VulkanRenderer renderer = new(vulkanResourceManager, renderTarget, renderPasses);

		renderers.Add(renderer);
		return renderer;
	}

	private void SetupGlfw() {
		GLFW.SetErrorCallback(ErrorCallback);
		GLFW.Init();

		Logger.Debug($"- Glfw Version: {GLFW.GetVersionString()}");
	}

	private void SetupVulkan() {
		vulkanResourceManager = new();

		// TODO vulkan
		// TODO print version
	}

	protected sealed override void InternalCleanup() {
		base.InternalCleanup();

		if (IsGlfwEnabled) {
			Logger.Trace("Cleaning up Glfw");

			GLFW.Terminate();
			GLFW.SetErrorCallback(null);
		}

		if (IsVulkanEnabled) {
			Logger.Trace("Cleaning up Vulkan");

			if (vulkanResourceManager == null) { throw new IllegalStateException(); }

			vulkanResourceManager.Cleanup();
		}
	}

	private static readonly Logger GlfwLogger = LoggerH.GetLogger(LogSource.Glfw);
	private static void ErrorCallback(ErrorCode error, string description) => GlfwLogger.Error($"[{error}] {description}");
}