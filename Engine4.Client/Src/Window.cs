using Silk.NET.GLFW;

namespace Engine4.Client;

// TODO glfwWindowShouldClose(window)

public unsafe class Window {
	private readonly Glfw glfw;
	private readonly WindowHandle* handle;

	public Window(Glfw glfw, string title, ushort width, ushort height) {
		this.glfw = glfw;

		// TODO way of setting hints? or just setting values once window is created

		glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
		glfw.WindowHint(WindowHintBool.Decorated, true);

		handle = glfw.CreateWindow(width, height, title, null, null);
		glfw.DefaultWindowHints(); // reset hints

		// https://github.com/glfw/glfw/issues/1398
		// TODO looks like wayland requires a buffer to "draw" the window
		// glfw.CreateWindowSurface();
	}

	public void Show() => glfw.ShowWindow(handle);
	public void Hide() => glfw.HideWindow(handle);

	private void Cleanup() {
		// TODO
	}
}