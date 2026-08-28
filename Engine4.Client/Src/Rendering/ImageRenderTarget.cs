using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public class ImageRenderTarget : RenderTarget {
	public ImageRenderTarget(GraphicsApi graphicsApi, IGraphicsApiProvider graphicsProvider) : base(graphicsApi, graphicsProvider) { }
}