using Engine4.Client.Graphics;
using Engine4.Client.Graphics.OpenGL;
using Engine4.Client.Graphics.Software;
using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.IO;
using Engine4.Client.Rendering;
using JetBrains.Annotations;
using OpenTK.Platform;
using GraphicsApi = Engine4.Client.Graphics.GraphicsApi;

namespace Engine4.Client;

public abstract class GameClient : Game {
	internal OpenGLGraphicsProvider? OpenGLGraphicsProvider { get; private set; }
	internal VulkanGraphicsProvider? VulkanGraphicsProvider { get; private set; }
	internal SoftwareGraphicsProvider? SoftwareGraphicsProvider { get; private set; }

	private List<Window> Windows { get; } = new(); // TODO cleanup
	private List<Renderer> Renderers { get; } = new(); // TODO cleanup

	protected readonly IReadOnlyList<Window> ReadonlyWindows;

	public GraphicsApis EnabledGraphicsApis { get; }
	public bool InitializeOpenTK { get; }

	protected GameClient(Engine engine, string name, GraphicsApis enabledGraphicsApis, bool initializeOpenTK) : base(engine, name, initializeOpenTK ? new OpenTKEventHandler() : null) {
		EnabledGraphicsApis = enabledGraphicsApis;
		InitializeOpenTK = initializeOpenTK;
		ReadonlyWindows = Windows.AsReadOnly();
	}

	protected override void Update() { }

	protected override void InternalSetup() {
		if (InitializeOpenTK) { SetupToolkit(Name); }
		SetupGraphicsApis();
	}

	private void SetupToolkit(string appName) {
		ToolkitFlags toolkitFlags = ToolkitFlags.None;

		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.OpenGL)) { toolkitFlags |= ToolkitFlags.EnableOpenGL; }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Vulkan)) { toolkitFlags |= ToolkitFlags.EnableVulkan; }

		Toolkit.Event.EventRaised += OnOpenTkEvent; // TODO cleanup

		Toolkit.Init(new() { ApplicationName = appName, FeatureFlags = toolkitFlags, Logger = new TkLogger(),}); // TODO cleanup
	}

	private void SetupGraphicsApis() {
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.OpenGL)) { OpenGLGraphicsProvider = new(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Vulkan)) { VulkanGraphicsProvider = new(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Software)) { SoftwareGraphicsProvider = new(); }

		// TODO more
	}

	[MustUseReturnValue]
	protected Window CreateWindow(GraphicsApi graphicsApi, string title, ushort width, ushort height) {
		// TODO validate?

		if (!InitializeOpenTK) { throw new Exception(); } // TODO exception

		Window window = new(graphicsApi, graphicsApi switch {
				GraphicsApi.None => throw new Exception(), // TODO exception
				GraphicsApi.OpenGL => new OpenGLGraphicsApiHints(), // TODO allow user control
				GraphicsApi.Vulkan => new VulkanGraphicsApiHints(), // ^
				GraphicsApi.Software => new SoftwareGraphicsApiHints(), // ^
				_ => throw new ArgumentOutOfRangeException(nameof(graphicsApi), graphicsApi, null),
		}, title, width, height); // TODO exception

		Windows.Add(window);

		return window;
	}

	[MustUseReturnValue]
	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		// TODO validate?

		Renderer renderer = renderTarget.GraphicsApi switch {
				GraphicsApi.None => throw new Exception(), // TODO exception
				GraphicsApi.OpenGL => new OpenGLRenderer(renderTarget, renderTarget.GraphicsProvider as OpenGLGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Vulkan => new VulkanRenderer(renderTarget, renderTarget.GraphicsProvider as VulkanGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Software => new SoftwareRenderer(renderTarget, renderTarget.GraphicsProvider as SoftwareGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				_ => throw new ArgumentOutOfRangeException(),
		};

		Renderers.Add(renderer);

		return renderer;
	}

	protected override void TryFreeResources() {
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

	private static void OnOpenTkEvent(EventArgs args) {
		switch (args) { // TODO add more
			case CloseEventArgs closeArgs:
				if (closeArgs.Window.UserData is not Window window) { throw new Exception(); } // TODO exception

				window.RequestClose();
				break;
			default: break;
		}
	}
}