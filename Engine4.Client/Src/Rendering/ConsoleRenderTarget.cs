using Engine4.Client.Graphics;

namespace Engine4.Client.Rendering;

public class ConsoleRenderTarget : RenderTarget {
	private readonly ConsoleRenderer consoleRenderer;

	public ConsoleRenderTarget(ConsoleRenderer consoleRenderer) => this.consoleRenderer = consoleRenderer;

	public void PresentFrame() => throw new NotImplementedException(); // TODO
}