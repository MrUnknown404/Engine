using Engine4.Client.Graphics.Console;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.IO;
using Engine4.Client.Rendering;

namespace Engine4.Client;

public abstract class GameClient : Game2 {
	public bool IsGlfwEnabled { get; } // allows windows
	public bool IsVulkanEnabled { get; } // allows window/image render target

	private readonly Lazy<Action>? lazyPollEvents;
	protected sealed override Action? PollEvents => lazyPollEvents?.Value ?? null;

	// windowing & graphics
	private readonly List<Window> windows = new(); // TODO cleanup. allow removal
	private readonly List<Renderer> renderers = new(); // TODO cleanup. allow removal

	private GlfwHandler? glfwHandler;
	private VulkanProvider? vulkanGraphicsProvider;
	private readonly ConsoleGraphicsProvider consoleGraphicsProvider = new();

	protected GameClient(string[] args, string name, bool useGlfw, bool useVulkan) : base(args, name) {
		IsGlfwEnabled = useGlfw;
		IsVulkanEnabled = useVulkan;

		if (useGlfw) { lazyPollEvents = new(() => (glfwHandler ?? throw new Exception()).Glfw.PollEvents); } // TODO exception
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

		Window window = new((glfwHandler ?? throw new Exception()).Glfw, title, width, height); // TODO exception
		windows.Add(window);
		return window;
	}

	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		Renderer renderer;

		if (renderTarget is ConsoleRenderTarget { UseVulkan: false, } consoleRenderTarget) {
			renderer = new ConsoleRenderer(consoleRenderTarget, consoleGraphicsProvider ?? throw new Exception(), renderPasses); // TODO exception
		} else {
			renderer = new VulkanRenderer(renderTarget, vulkanGraphicsProvider ?? throw new Exception(), renderPasses); // TODO exception
		}

		renderers.Add(renderer);
		return renderer;
	}

	protected sealed override void SetupInternals() {
		base.SetupInternals();

		SetupWindowing();
		SetupGraphics();

		// TODO
	}

	private void SetupWindowing() {
		// TODO

		if (IsGlfwEnabled) { glfwHandler = new(); }
	}

	private void SetupGraphics() {
		// TODO

		if (IsVulkanEnabled) { SetupVulkan(); }
	}

	private void SetupVulkan() {
		// TODO

		vulkanGraphicsProvider = new();
	}

	protected sealed override void InternalCleanup() {
		base.InternalCleanup();

		if (glfwHandler != null) {
			glfwHandler.Cleanup();
			glfwHandler = null;
		}

		// TODO
	}
}