namespace Engine4.Graphics;

public class GraphicsApi {
	public static readonly GraphicsApi None = new(NoEvents);
	public static readonly GraphicsApi OpenGL = new(TryProcessOpenTKEvents);
	public static readonly GraphicsApi Vulkan = new(TryProcessOpenTKEvents);
	public static readonly GraphicsApi Software = new(NoEvents);

	public Action TryProcessEvents { get; }

	private GraphicsApi(Action tryProcessEvents) => TryProcessEvents = tryProcessEvents;

	private static void NoEvents() { }
	private static void TryProcessOpenTKEvents() { } // TODO
}