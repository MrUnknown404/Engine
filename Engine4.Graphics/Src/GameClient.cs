using Engine4.Graphics.IO;
using Engine4.Graphics.OpenGL;
using Engine4.Graphics.Rendering;
using Engine4.Graphics.Software;
using Engine4.Graphics.Vulkan;
using Engine4.Graphics.Windowing;
using JetBrains.Annotations;
using OpenTK.Platform;

namespace Engine4.Graphics;

public abstract class GameClient : Game {
	internal OpenGLGraphicsProvider? OpenGLGraphicsProvider { get; private set; }
	internal VulkanGraphicsProvider? VulkanGraphicsProvider { get; private set; }
	internal SoftwareGraphicsProvider? SoftwareGraphicsProvider { get; private set; }

	private List<Window> Windows { get; } = new(); // TODO cleanup/try to close
	private List<Renderer> Renderers { get; } = new(); // TODO cleanup

	public GraphicsApis EnabledGraphicsApis { get; }
	public bool InitializeOpenTK { get; }

	protected GameClient(Engine engine, string name, GraphicsApis enabledGraphicsApis, bool initializeOpenTK) : base(engine, name, initializeOpenTK ? new OpenTKEventHandler() : null) {
		EnabledGraphicsApis = enabledGraphicsApis;
		InitializeOpenTK = initializeOpenTK;
	}

	protected override void Update() { }

	protected override void InternalSetup() {
		if (InitializeOpenTK) { SetupToolkit(Name); }
		SetupGraphicsApis();
	}

	private void SetupToolkit(string appName) {
		// TODO

		ToolkitFlags toolkitFlags = ToolkitFlags.None;

		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.OpenGL)) { toolkitFlags |= ToolkitFlags.EnableOpenGL; }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Vulkan)) { toolkitFlags |= ToolkitFlags.EnableVulkan; }

		Toolkit.Event.EventRaised += OnOpenTkEvent;

		Toolkit.Init(new() { ApplicationName = appName, FeatureFlags = toolkitFlags, });

		return;

		void OnOpenTkEvent(EventArgs args) { } // TODO
	}

	private void SetupGraphicsApis() {
		// TODO

		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.OpenGL)) { OpenGLGraphicsProvider = new(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Vulkan)) { VulkanGraphicsProvider = new(); }
		if (EnabledGraphicsApis.HasFlagFast(GraphicsApis.Software)) { SoftwareGraphicsProvider = new(); }
	}

	[MustUseReturnValue]
	protected Window CreateWindow(GraphicsApi graphicsApi, string title, ushort width, ushort height) {
		// TODO validate?

		// TODO assuming windows are opentk only. this may change
		if (EventHandler is not OpenTKEventHandler) { throw new Exception(); } // TODO exception

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
		foreach (Window window in Windows) {
			if (window.ShouldClose) {
				window.Destroy();
				Windows.Remove(window); // TODO fix
			}
		}
	}
}