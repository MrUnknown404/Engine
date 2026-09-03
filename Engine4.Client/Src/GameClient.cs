using Engine4.Client.Graphics;
using Engine4.Client.Graphics.OpenGL;
using Engine4.Client.Graphics.Software;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Rendering;
using JetBrains.Annotations;

namespace Engine4.Client;

public abstract class GameClient : Game {
	internal NoGraphicsProvider? NoneGraphicsProvider { get; private set; }
	internal OpenGLGraphicsProvider? OpenGLGraphicsProvider { get; private set; }
	internal VulkanGraphicsProvider? VulkanGraphicsProvider { get; private set; }
	internal SoftwareGraphicsProvider? SoftwareGraphicsProvider { get; private set; }

	private List<Window> Windows { get; } = new(); // TODO cleanup
	private List<Renderer> Renderers { get; } = new(); // TODO cleanup

	protected readonly IReadOnlyList<Window> ReadonlyWindows;

	public GraphicsApis EnabledGraphicsApis { get; }

	protected GameClient(Engine engine, string name, GraphicsApis enabledGraphicsApis) : base(engine, name, null) {
		EnabledGraphicsApis = enabledGraphicsApis;
		ReadonlyWindows = Windows.AsReadOnly();
	}

	protected override void Update() { }

	protected override void InternalSetup() { SetupGraphicsApis(); }

	private void SetupGraphicsApis() {
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.OpenGL)) { OpenGLGraphicsProvider = new(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Vulkan)) { VulkanGraphicsProvider = new(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Software)) { SoftwareGraphicsProvider = new(); }

		// TODO more
	}

	[MustUseReturnValue]
	protected Window CreateWindow(GraphicsApi graphicsApi, string title, ushort width, ushort height) {
		// if (!InitializeOpenTK) { throw new Engine4Exception("Cannot create a new window if OpenTK is not initialized"); } TODO

		Window window = new(graphicsApi, title, width, height);
		Windows.Add(window);
		return window;
	}

	[MustUseReturnValue]
	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		// TODO validate target and passes?

		Renderer renderer = renderTarget.GraphicsApi switch {
				GraphicsApi.None => new EmptyRenderer(renderTarget, renderTarget.GraphicsProvider as NoGraphicsProvider ?? throw new Exception()), // TODO exception
				GraphicsApi.OpenGL => new OpenGLRenderer(renderTarget, renderTarget.GraphicsProvider as OpenGLGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Vulkan => new VulkanRenderer(renderTarget, renderTarget.GraphicsProvider as VulkanGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Software => new SoftwareRenderer(renderTarget, renderTarget.GraphicsProvider as SoftwareGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				_ => throw new ArgumentOutOfRangeException(),
		};

		Renderers.Add(renderer);

		return renderer;
	}

	protected override void TryFreeResources() {
		// windows
		for (int i = 0; i < Windows.Count; i++) {
			Window window = Windows[i];

			if (window.ShouldClose) {
				// TODO check if window has an associated renderer. clean that first

				window.Destroy();
				Windows.RemoveAt(i);
				i--;
			}
		}
	}

	protected override void Cleanup() {
		// TODO cleanup everything

		// TODO cleanup windows

		OpenGLGraphicsProvider?.Cleanup();
		VulkanGraphicsProvider?.Cleanup();
		SoftwareGraphicsProvider?.Cleanup();
	}
}