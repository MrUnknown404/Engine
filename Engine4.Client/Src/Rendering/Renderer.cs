using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public abstract class Renderer {
	public IGraphicsProvider GraphicsProvider { get; }
	public ulong FrameCount { get; private set; }

	protected List<RenderPass> RenderPasses { get; } // TODO make sure this supports adding/removing at runtime
	protected RenderTarget RenderTarget { get; } // TODO eventually allow multiple targets

	protected Renderer(RenderTarget renderTarget, IGraphicsProvider graphicsProvider, params RenderPass[] renderPasses) {
		RenderTarget = renderTarget;
		GraphicsProvider = graphicsProvider;
		RenderPasses = new(renderPasses);
	}

	internal void InternalRender(float delta) {
		Render(delta);
		FrameCount++;
	}

	protected abstract void Render(float delta);
}