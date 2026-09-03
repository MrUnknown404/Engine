using Engine4.Client.Rendering;

namespace Engine4.Client.Graphics.Console;

public class ConsoleRenderer : Renderer {
	// this'll need to disable regular console output

	public ConsoleRenderer(ConsoleRenderTarget renderTarget, IGraphicsProvider graphicsProvider, params RenderPass[] renderPasses) : base(renderTarget, graphicsProvider, renderPasses) { }

	public override bool BeginFrame() => throw new NotImplementedException(); // TODO
	public override void UpdateBuffers(float delta) => throw new NotImplementedException(); // TODO
	public override void DrawFrame() => throw new NotImplementedException(); // TODO
	public override void EndFrame() => throw new NotImplementedException(); // TODO
	public override void PresentFrame() => throw new NotImplementedException(); // TODO
}