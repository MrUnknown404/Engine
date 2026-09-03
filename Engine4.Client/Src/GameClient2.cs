using Engine4.Client.Graphics;
using Engine4.Client.Graphics.OpenGL;
using Engine4.Client.Graphics.Software;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.IO;
using Engine4.Client.Rendering;

namespace Engine4.Client;

public abstract class GameClient2 : Game2 {
	public bool IsGlfwEnabled { get; }
	public GraphicsApis EnabledGraphicsApis { get; }

	private readonly Lazy<Action>? lazyPollEvents;
	protected sealed override Action? PollEvents => lazyPollEvents?.Value ?? null;

	// windowing & graphics providers
	private GlfwHandler? glfwHandler;

	private readonly NoGraphicsProvider noGraphicsProvider = new();
	private OpenGLGraphicsProvider? openGLGraphicsProvider;
	private VulkanGraphicsProvider? vulkanGraphicsProvider;
	private SoftwareGraphicsProvider? softwareGraphicsProvider;

	protected GameClient2(string[] args, string name, bool useGlfw, GraphicsApis graphicsApis) : base(args, name) {
		IsGlfwEnabled = useGlfw;
		EnabledGraphicsApis = graphicsApis;

		if (useGlfw) { lazyPollEvents = new(() => (glfwHandler ?? throw new Exception()).Glfw.PollEvents); } // TODO exception
	}

	protected sealed override void InternalUpdate() {
		base.InternalUpdate();
		// TODO
	}

	protected Window2 CreateWindow(GraphicsApi graphicsApi, string title, ushort width, ushort height) {
		// TODO

		if (!IsGlfwEnabled) { throw new Exception(); } // TODO exception

		Window2 window = new((glfwHandler ?? throw new Exception()).Glfw, graphicsApi, title, width, height); // TODO exception
		return window; // TODO cleanup
	}

	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		// TODO

		Renderer renderer = renderTarget.GraphicsApi switch {
				GraphicsApi.None => new EmptyRenderer(renderTarget, noGraphicsProvider, renderPasses),
				GraphicsApi.OpenGL => new OpenGLRenderer(renderTarget, openGLGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Vulkan => new VulkanRenderer(renderTarget, vulkanGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Software => new SoftwareRenderer(renderTarget, softwareGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				_ => throw new ArgumentOutOfRangeException(),
		};

		return renderer; // TODO cleanup
	}

	public IGraphicsApiProvider GetGraphicsProvider(GraphicsApi graphicsApi) { // do i want this?
		// TODO

		return graphicsApi switch {
				GraphicsApi.None => noGraphicsProvider,
				GraphicsApi.OpenGL => openGLGraphicsProvider ?? throw new Exception(), // TODO exception
				GraphicsApi.Vulkan => vulkanGraphicsProvider ?? throw new Exception(), // TODO exception
				GraphicsApi.Software => softwareGraphicsProvider ?? throw new Exception(), // TODO exception
				_ => throw new ArgumentOutOfRangeException(nameof(graphicsApi), graphicsApi, null),
		};
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

		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.OpenGL)) { SetupOpenGL(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Vulkan)) { SetupVulkan(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Software)) { SetupSoftwareGraphics(); }
	}

	private void SetupOpenGL() {
		// TODO

		openGLGraphicsProvider = new();
	}

	private void SetupVulkan() {
		// TODO

		vulkanGraphicsProvider = new();
	}

	private void SetupSoftwareGraphics() {
		// TODO

		softwareGraphicsProvider = new();
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