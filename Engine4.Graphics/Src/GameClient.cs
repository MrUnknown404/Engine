using Engine4.Graphics.OpenGL;
using Engine4.Graphics.Rendering;
using Engine4.Graphics.Software;
using Engine4.Graphics.Vulkan;
using Engine4.Graphics.Windowing;
using Engine4.IO;
using JetBrains.Annotations;
using OpenTK.Platform;

namespace Engine4.Graphics;

public abstract class GameClient : Game {
	public IGraphicsApiProvider? GraphicsProvider { get; } // null when GraphicsApi is None

	internal List<Window> Windows { get; } = new(); // TODO cleanup/try to close
	internal List<Renderer> Renderers { get; } = new(); // TODO cleanup

	private readonly GraphicsApiHints? graphicsApiHints;

	protected GameClient(Engine engine, GraphicsApiHints? graphicsApiHints) : base(engine) {
		this.graphicsApiHints = graphicsApiHints;
		GraphicsProvider = engine.GraphicsApi switch {
				GraphicsApi.None => null,
				GraphicsApi.OpenGL => new OpenGLGraphicsProvider(),
				GraphicsApi.Vulkan => new VulkanGraphicsProvider(),
				GraphicsApi.Software => new SoftwareGraphicsProvider(),
				_ => throw new ArgumentOutOfRangeException(),
		};
	}

	[MustUseReturnValue]
	protected Window CreateWindow(string title, ushort width, ushort height) {
		// TODO validate?

		// TODO assuming windows are opentk only. this may change
		if (Engine.EventHandler is not OpenTKEventHandler opentkEventHandler) { throw new Exception(); } // TODO exception

		Window window = new(graphicsApiHints ?? throw new Exception(), title, width, height); // TODO exception

		Windows.Add(window);
		opentkEventHandler.RegisterWindow(window); // TODO should this be render target

		return window;
	}

	[MustUseReturnValue]
	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		// TODO validate?

		Renderer renderer = Engine.GraphicsApi switch {
				GraphicsApi.None => throw new Exception(), // TODO throw error
				GraphicsApi.OpenGL => new OpenGLRenderer(renderTarget, GraphicsProvider as OpenGLGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Vulkan => new VulkanRenderer(renderTarget, GraphicsProvider as VulkanGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				GraphicsApi.Software => new SoftwareRenderer(renderTarget, GraphicsProvider as SoftwareGraphicsProvider ?? throw new Exception(), renderPasses), // TODO exception
				_ => throw new ArgumentOutOfRangeException(),
		};

		Renderers.Add(renderer);

		return renderer;
	}

	protected override void TryFreeResources() {
		foreach (Window window in Windows) { }
	}
}