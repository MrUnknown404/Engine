using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public abstract class RenderTarget {
	public GraphicsApi GraphicsApi { get; }
	public IGraphicsApiProvider GraphicsProvider { get; }

	protected RenderTarget(GraphicsApi graphicsApi, IGraphicsApiProvider graphicsProvider) {
		GraphicsApi = graphicsApi;
		GraphicsProvider = graphicsProvider;
	}
}