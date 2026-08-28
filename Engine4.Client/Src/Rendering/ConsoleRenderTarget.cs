using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public class ConsoleRenderTarget : RenderTarget {
	public ConsoleRenderTarget(GraphicsApi graphicsApi, IGraphicsApiProvider graphicsProvider) : base(graphicsApi, graphicsProvider) { }
}