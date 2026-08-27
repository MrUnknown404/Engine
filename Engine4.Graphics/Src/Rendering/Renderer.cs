namespace Engine4.Graphics.Rendering;

public abstract class Renderer {
	public GraphicsApi GraphicsApi { get; }

	protected RenderTarget RenderTarget { get; }
	protected IGraphicsApiProvider GraphicsProvider { get; }

	public List<RenderPass> RenderPasses { get; }

	protected Renderer(GraphicsApi graphicsApi, RenderTarget renderTarget, IGraphicsApiProvider graphicsProvider, params RenderPass[] renderPasses) {
		GraphicsApi = graphicsApi;
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