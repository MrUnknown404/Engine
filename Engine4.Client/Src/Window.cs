using Engine4.Client.Graphics;

namespace Engine4.Client;

public class Window {
	// TODO should windows be comparable?

	public GraphicsApi GraphicsApi { get; }
	public bool ShouldClose { get; set; }

	public event TryCloseWindowDelegate? TryCloseWindowEvent;

	public event Action? OnWindowClosedEvent;

	internal Window(GraphicsApi graphicsApi, string title, ushort width, ushort height) { GraphicsApi = graphicsApi; }

	public void Hide() => throw new NotImplementedException(); // TODO
	public void Show() => throw new NotImplementedException(); // TODO

	public void RequestClose() {
		bool shouldClose = true;
		TryCloseWindowEvent?.Invoke(ref shouldClose);
		if (shouldClose) { ShouldClose = true; }
	}

	internal void Destroy() {
		OnWindowClosedEvent?.Invoke();

		// TODO
	}

	public delegate void TryCloseWindowDelegate(ref bool shouldClose);
}