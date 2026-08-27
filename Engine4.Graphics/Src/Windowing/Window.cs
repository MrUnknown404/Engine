using OpenTK.Platform;

namespace Engine4.Graphics.Windowing;

public class Window {
	// TODO input is window specific
	// TODO should windows be comparable?
	// TODO opentk only?

	public GraphicsApi GraphicsApi { get; }

	protected WindowHandle Handle { get; }

	public bool ShouldClose { get; set; } // TODO use

	public event TryCloseWindowDelegate? TryCloseWindowEvent;

	public event Action? OnWindowClosedEvent; // TODO call

	internal Window(GraphicsApi graphicsApi, GraphicsApiHints graphicsApiHints, string title, ushort width, ushort height) {
		GraphicsApi = graphicsApi;
		Handle = Toolkit.Window.Create(graphicsApiHints);
		Toolkit.Window.SetTitle(Handle, title);
		Toolkit.Window.SetSize(Handle, new(width, height));
	}

	public void Show() => throw new NotImplementedException(); // TODO

	public void RequestClose() {
		bool shouldClose = true;
		TryCloseWindowEvent?.Invoke(ref shouldClose);
		if (shouldClose) { ShouldClose = true; }
	}

	public delegate void TryCloseWindowDelegate(ref bool shouldClose);
}