using Engine4.Client.Rendering;

namespace Engine4.Client.Graphics;

public class EmptyRenderer : Renderer {
	public EmptyRenderer(RenderTarget renderTarget, NoGraphicsProvider graphicsProvider, params RenderPass[] renderPasses) : base(GraphicsApi.None, renderTarget, graphicsProvider, renderPasses) { }

	public override bool BeginFrame() => true;
	public override void UpdateBuffers(float delta) { }
	public override void DrawFrame() { }
	public override void EndFrame() { }
	public override void PresentFrame() { }
}