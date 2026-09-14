using OpenTK.Windowing.GraphicsLibraryFramework;
using GlfwWindow = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Engine4.Client;

public unsafe class Window {
	internal GlfwWindow* GlfwWindow { get; } // TODO private

	public bool ShouldClose { get; private set; }

	public event RequestCloseDelegate? RequestCloseEvent;

	internal Window(string title, ushort width, ushort height) {
		// TODO way of setting hints? or just setting values once the window is created
		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi); // disable opengl
		GLFW.WindowHint(WindowHintBool.Decorated, true);

		GlfwWindow = GLFW.CreateWindow(width, height, title, null, null);
		GLFW.DefaultWindowHints(); // reset hints

		// TODO looks like wayland requires you to draw once before the window will appear. see https://github.com/glfw/glfw/issues/1398
	}

	public void Show() => GLFW.ShowWindow(GlfwWindow);
	public void Hide() => GLFW.HideWindow(GlfwWindow);

	public void RequestClose(bool force) {
		if (force) {
			ShouldClose = true;
			return;
		}

		bool shouldClose = true;
		RequestCloseEvent?.Invoke(ref shouldClose);
		if (shouldClose) { ShouldClose = true; }
	}

	internal bool GlfwShouldClose() => GLFW.WindowShouldClose(GlfwWindow);

	internal void Cleanup() => GLFW.DestroyWindow(GlfwWindow); // TODO why isn't this working?

	public delegate bool RequestCloseDelegate(ref bool shouldShutdown);
}