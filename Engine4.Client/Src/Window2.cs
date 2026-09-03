using Engine4.Client.Graphics;
using Silk.NET.GLFW;

namespace Engine4.Client;

// TODO glfwWindowShouldClose(window)

public unsafe class Window2 {
	public GraphicsApi GraphicsApi { get; }

	private readonly Glfw glfw;
	private readonly WindowHandle* handle;

	public Window2(Glfw glfw, GraphicsApi graphicsApi, string title, ushort width, ushort height) {
		this.glfw = glfw;
		GraphicsApi = graphicsApi;

		// TODO way of setting hints? or just setting values once window is created

		glfw.WindowHint(WindowHintClientApi.ClientApi, graphicsApi switch {
				GraphicsApi.None or GraphicsApi.Vulkan or GraphicsApi.Software => ClientApi.NoApi,
				GraphicsApi.OpenGL => ClientApi.OpenGL,
				_ => throw new ArgumentOutOfRangeException(nameof(graphicsApi), graphicsApi, null),
		});

		glfw.WindowHint(WindowHintBool.Decorated, true);

		handle = glfw.CreateWindow(width, height, title, null, null);
		glfw.DefaultWindowHints(); // reset

		// https://github.com/glfw/glfw/issues/1398
		// TODO TEMP. looks like wayland requires a buffer to "draw" the window
		switch (graphicsApi) {
			case GraphicsApi.None: break; // TODO how?
			case GraphicsApi.OpenGL:
				glfw.MakeContextCurrent(handle);
				glfw.SwapBuffers(handle);
				break;
			case GraphicsApi.Vulkan:
				// glfw.CreateWindowSurface(); //
				break;
			case GraphicsApi.Software: break; // TODO how?
			default: throw new ArgumentOutOfRangeException(nameof(graphicsApi), graphicsApi, null);
		}
	}

	public void Show() => glfw.ShowWindow(handle);
	public void Hide() => glfw.HideWindow(handle);

	private void Cleanup() {
		// TODO
	}
}