using Engine4.Graphics.OpenGL;
using Engine4.Graphics.Rendering;
using Engine4.Graphics.Software;
using Engine4.Graphics.Vulkan;
using Engine4.Graphics.Windowing;
using JetBrains.Annotations;

namespace Engine4.Graphics;

public abstract class GameClient : Game {
	public IGraphicsApiProvider? GraphicsProvider { get; } // null when GraphicsApi is None

	protected GameClient(Engine engine) : base(engine) {
		GraphicsProvider = engine.GraphicsApi switch {
				GraphicsApi.None => null,
				GraphicsApi.OpenGL => new OpenGLGraphicsProvider(),
				GraphicsApi.Vulkan => new VulkanGraphicsProvider(),
				GraphicsApi.Software => new SoftwareGraphicsProvider(),
				_ => throw new ArgumentOutOfRangeException(),
		};
	}

	[MustUseReturnValue]
	protected Window CreateWindow() {
		if (Engine.GraphicsApi == GraphicsApi.None) { throw new Exception(); } // TODO exception

		Window window = new(); // todo create event handler and store
		return window;
	}

	[MustUseReturnValue]
	protected Renderer CreateRenderer(RenderTarget renderTarget, params RenderPass[] renderPasses) {
		Renderer renderer = Engine.GraphicsApi switch {
				GraphicsApi.None => throw new Exception(), // TODO throw error
				GraphicsApi.OpenGL => new OpenGLRenderer(renderTarget, GraphicsProvider!, renderPasses),
				GraphicsApi.Vulkan => new VulkanRenderer(renderTarget, GraphicsProvider!, renderPasses),
				GraphicsApi.Software => new SoftwareRenderer(renderTarget, GraphicsProvider!, renderPasses),
				_ => throw new ArgumentOutOfRangeException(),
		}; // todo store

		return renderer;
	}
}