using OpenTK.Windowing.GraphicsLibraryFramework;
using GlfwWindow = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Engine4.Client;

public unsafe class Window {
	private readonly GlfwWindow* glfwWindow;

	internal Window(string title, ushort width, ushort height) {
		// TODO way of setting hints? or just setting values once window is created

		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
		GLFW.WindowHint(WindowHintBool.Decorated, true);

		glfwWindow = GLFW.CreateWindow(width, height, title, null, null);
		GLFW.DefaultWindowHints(); // reset hints

		// https://github.com/glfw/glfw/issues/1398
		// TODO looks like wayland requires a buffer to "draw" the window
		// TODO glfw.CreateWindowSurface
	}

	public void Show() => GLFW.ShowWindow(glfwWindow);
	public void Hide() => GLFW.HideWindow(glfwWindow);

	private void Cleanup() {
		// TODO
	}
}