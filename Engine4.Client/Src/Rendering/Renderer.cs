using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public abstract class Renderer {
	public IGraphicsProvider GraphicsProvider { get; }
	public List<RenderPass> RenderPasses { get; }

	protected RenderTarget RenderTarget { get; }

	protected Renderer(RenderTarget renderTarget, IGraphicsProvider graphicsProvider, params RenderPass[] renderPasses) {
		RenderTarget = renderTarget;
		GraphicsProvider = graphicsProvider;
		RenderPasses = new(renderPasses);
	}

	protected internal abstract bool BeginFrame();
	protected internal abstract void UpdateBuffers(float delta);
	protected internal abstract void DrawFrame();
	protected internal abstract void EndFrame();
	protected internal abstract void PresentFrame();
}