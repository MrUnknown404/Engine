using Engine3.Core;
using Engine3.Core.Utility;

namespace Engine3.Client.Client.Graphics;

public abstract class GraphicsBackend {
	public GraphicsApi GraphicsApi { get; }

	protected GraphicsBackend(GraphicsApi graphicsApi) => GraphicsApi = graphicsApi;

	protected internal abstract void Setup(EngineGame game);
	protected internal abstract void Cleanup();
}