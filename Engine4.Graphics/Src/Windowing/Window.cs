using OpenTK.Platform;

namespace Engine4.Graphics.Windowing;

public class Window {
	// TODO input is window specific
	// TODO should windows be comparable?
	// TODO opentk only?

	public GraphicsApi GraphicsApi { get; }
	public bool ShouldClose { get; set; }

	private readonly WindowHandle handle;

	public event TryCloseWindowDelegate? TryCloseWindowEvent;

	public event Action? OnWindowClosedEvent;

	internal Window(GraphicsApi graphicsApi, GraphicsApiHints graphicsApiHints, string title, ushort width, ushort height) {
		GraphicsApi = graphicsApi;
		handle = Toolkit.Window.Create(graphicsApiHints);
		handle.UserData = this;

		Toolkit.Window.SetTitle(handle, title);
		Toolkit.Window.SetSize(handle, new(width, height));
	}

	public void Hide() => Toolkit.Window.SetMode(handle, WindowMode.Hidden);
	public void Show() => Toolkit.Window.SetMode(handle, WindowMode.Normal);

	public void RequestClose() {
		bool shouldClose = true;
		TryCloseWindowEvent?.Invoke(ref shouldClose);
		if (shouldClose) { ShouldClose = true; }
	}

	internal void Destroy() {
		OnWindowClosedEvent?.Invoke();

		Toolkit.Window.Destroy(handle);
	}

	public delegate void TryCloseWindowDelegate(ref bool shouldClose);
}