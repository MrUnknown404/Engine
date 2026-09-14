namespace Engine4.Client.Rendering;

public class ConsoleRenderTarget : RenderTarget {
	private readonly ConsoleRenderer consoleRenderer;

	internal ConsoleRenderTarget(ConsoleRenderer consoleRenderer) => this.consoleRenderer = consoleRenderer;

	public void PresentFrame() => throw new NotImplementedException(); // TODO

	protected internal override void Cleanup() { }
}