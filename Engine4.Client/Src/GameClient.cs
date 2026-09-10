using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Rendering;
using Engine4.IO;
using Engine4.Utility.Versions;
using NLog;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine4.Client;

public abstract class GameClient : GameCore {
	private static readonly Logger Logger = LoggerH.GetLogger(LogSource.Game);

	public bool IsGlfwEnabled { get; private set; }
	public bool IsVulkanEnabled { get; private set; }

	protected sealed override Action? PollEvents { get; }

	// windowing & graphics
	private readonly List<Window> windows = new(); // TODO cleanup. allow removal
	private readonly List<Renderer> renderers = new(); // TODO cleanup. allow removal

	private VulkanResourceManager? vulkanResourceManager;

	protected GameClient(string name, IPackableVersion version) : base(name, version) {
		if (IsGlfwEnabled) { PollEvents = GLFW.PollEvents; }
	}

	protected sealed override void SetupInternals(StartupSettings coreSettings) {
		if (coreSettings is not ClientStartupSettings clientSettings) { throw new Exception(); } // TODO exception

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

		// TODO
	}

	protected sealed override void InternalUpdate() {
		base.InternalUpdate(); // TODO anything?
	}

	protected sealed override void InternalRender(float delta) {
		base.InternalRender(delta); // TODO anything?
	}

	protected sealed override void Render(float delta) {
		foreach (Renderer renderer in renderers) { renderer.InternalRender(delta); }
	}

	protected Window CreateWindow(string title, ushort width, ushort height) {
		if (!IsGlfwEnabled) { throw new Exception(); } // TODO exception

		Logger.Debug("Creating window...");
		Window window = new(title, width, height); // TODO exception

		windows.Add(window);
		return window;
	}

	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		Logger.Debug("Creating renderer...");
		Renderer renderer = new VulkanRenderer(renderTarget, vulkanResourceManager ?? throw new Exception(), renderPasses); // TODO exception

		renderers.Add(renderer);
		return renderer;
	}

	private void SetupGlfw() {
		// TODO

		GLFW.SetErrorCallback(ErrorCallback);
		GLFW.Init();
	}

	private void SetupVulkan() {
		// TODO

		vulkanResourceManager = new();
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

			if (vulkanResourceManager == null) { throw new Exception(); } // TODO exception

			vulkanResourceManager.Cleanup();
		}

		// TODO
	}

	private static readonly Logger GlfwLogger = LoggerH.GetLogger(LogSource.Glfw);
	private static void ErrorCallback(ErrorCode error, string description) => GlfwLogger.Error($"[{error}] {description}");
}