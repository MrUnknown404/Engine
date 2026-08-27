namespace Engine4.Graphics.Rendering;

public abstract class RenderTarget {
	public GraphicsApi GraphicsApi { get; }
	public IGraphicsApiProvider GraphicsProvider { get; }

	protected RenderTarget(GraphicsApi graphicsApi, IGraphicsApiProvider graphicsProvider) {
		GraphicsApi = graphicsApi;
		GraphicsProvider = graphicsProvider;
	}
}