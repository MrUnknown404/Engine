using Engine4.Client.Graphics.Console;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Rendering;
using Engine4.Client.Utility;
using Engine4.Utility.Versions;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine4.Client;

public abstract class GameClient : GameCore {
	private readonly GameStartupFlags startupFlags;

	public bool IsGlfwEnabled => startupFlags.HasFlagFast(GameStartupFlags.UseGlfw);
	public bool IsVulkanEnabled => startupFlags.HasFlagFast(GameStartupFlags.UseVulkan);

	protected sealed override Action? PollEvents { get; }

	// windowing & graphics
	private readonly List<Window> windows = new(); // TODO cleanup. allow removal
	private readonly List<Renderer> renderers = new(); // TODO cleanup. allow removal

	private VulkanProvider? vulkanGraphicsProvider;
	private readonly ConsoleGraphicsProvider consoleGraphicsProvider = new();

	protected GameClient(string name, IPackableVersion version, GameStartupFlags startupFlags) : base(name, version) {
		this.startupFlags = startupFlags;

		if (IsGlfwEnabled) { PollEvents = GLFW.PollEvents; }
	}

	protected sealed override void SetupInternals() {
		base.SetupInternals();

		SetupWindowing();
		SetupGraphics();

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

		if (IsGlfwEnabled) {
			GLFW.SetErrorCallback(ErrorCallback);
			GLFW.Init();
		}
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

		if (IsGlfwEnabled) {
			GLFW.Terminate();
			GLFW.SetErrorCallback(null);
		}

		// TODO
	}

	private static void ErrorCallback(ErrorCode error, string description) => Console.WriteLine($"[GLFW] [{error}] {description}"); // TODO log
}