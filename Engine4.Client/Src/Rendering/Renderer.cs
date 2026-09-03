using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public abstract class Renderer {
	protected RenderTarget RenderTarget { get; }
	protected IGraphicsProvider GraphicsProvider { get; }

	public List<RenderPass> RenderPasses { get; }

	protected Renderer(RenderTarget renderTarget, IGraphicsProvider graphicsProvider, params RenderPass[] renderPasses) {
		RenderTarget = renderTarget;
		GraphicsProvider = graphicsProvider;
		RenderPasses = new(renderPasses);
	}

	public abstract bool BeginFrame();
	public abstract void UpdateBuffers(float delta);
	public abstract void DrawFrame();
	public abstract void EndFrame();
	public abstract void PresentFrame();
}