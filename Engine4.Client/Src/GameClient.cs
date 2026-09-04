using Engine4.Client.Graphics.Console;
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

	private VulkanProvider? vulkanGraphicsProvider;
	private readonly ConsoleGraphicsProvider consoleGraphicsProvider = new();

	protected GameClient(string name, IPackableVersion version) : base(name, version) {
		if (IsGlfwEnabled) { PollEvents = GLFW.PollEvents; }
	}

	protected sealed override void SetupInternals(StartupSettings settings) {
		base.SetupInternals(settings);

		if (settings.LoadGlfw) {
			IsGlfwEnabled = true;
			SetupWindowing();
		}

		if (settings.LoadVulkan) {
			IsVulkanEnabled = true;
			SetupVulkan();
		}

		// TODO
	}

	protected sealed override void InternalUpdate() {
		base.InternalUpdate();
		// TODO
	}

	protected sealed override void Render(float delta) {
		foreach (Renderer renderer in renderers) {
			if (renderer.BeginFrame()) {
				renderer.UpdateBuffers(delta);
				renderer.DrawFrame();
				renderer.EndFrame();
				renderer.PresentFrame();
			}
		}
	}

	protected Window CreateWindow(string title, ushort width, ushort height) {
		if (!IsGlfwEnabled) { throw new Exception(); } // TODO exception

		Window window = new(title, width, height); // TODO exception
		windows.Add(window);
		return window;
	}

	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		Renderer renderer = renderTarget is ConsoleRenderTarget { UseVulkan: false, } consoleRenderTarget ?
				new ConsoleRenderer(consoleRenderTarget, consoleGraphicsProvider ?? throw new Exception(), renderPasses) : // TODO exception
				new VulkanRenderer(renderTarget, vulkanGraphicsProvider ?? throw new Exception(), renderPasses); // TODO exception

		renderers.Add(renderer);
		return renderer;
	}

	private void SetupWindowing() {
		// TODO

		GLFW.SetErrorCallback(ErrorCallback);
		GLFW.Init();
	}

	private void SetupVulkan() {
		// TODO

		vulkanGraphicsProvider = new();
	}

	protected sealed override void InternalCleanup() {
		base.InternalCleanup();

		if (IsGlfwEnabled) {
			GLFW.Terminate();
			GLFW.SetErrorCallback(null);
		}

		if (IsVulkanEnabled) {
			// TODO cleanup vulkan
		}

		// TODO
	}

	private static readonly Logger GlfwLogger = LoggerH.GetLogger(LogSource.Glfw);
	private static void ErrorCallback(ErrorCode error, string description) => GlfwLogger.Error($"[{error}] {description}");
}