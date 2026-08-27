using Engine4.Graphics.Rendering;

namespace Engine4.Graphics.Software;

public class SoftwareRenderer : Renderer {
	internal SoftwareRenderer(RenderTarget renderTarget, SoftwareGraphicsProvider graphicsProvider, params RenderPass[] renderPasses) : base(GraphicsApi.Software, renderTarget, graphicsProvider, renderPasses) { }

	public override bool BeginFrame() => throw new NotImplementedException(); // TODO
	public override void UpdateBuffers(float delta) => throw new NotImplementedException(); // TODO
	public override void DrawFrame() => throw new NotImplementedException(); // TODO
	public override void EndFrame() => throw new NotImplementedException(); // TODO
	public override void PresentFrame() => throw new NotImplementedException(); // TODO
}