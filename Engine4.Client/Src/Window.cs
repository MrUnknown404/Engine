using Engine4.Client.Graphics.Vulkan;
using Engine4.Client.Graphics.Vulkan.Objects;
using OpenTK.Windowing.GraphicsLibraryFramework;
using GlfwWindow = OpenTK.Windowing.GraphicsLibraryFramework.Window;

namespace Engine4.Client;

public unsafe class Window {
	private readonly GlfwWindow* glfwWindow;
	private readonly VulkanSurface surface;

	private bool shouldClose; // TODO try close before update

	public event RequestCloseDelegate? RequestCloseEvent;

	internal Window(VulkanManager vulkanManager, string title, ushort width, ushort height) {
		// TODO way of setting hints? or just setting values once the window is created
		GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
		GLFW.WindowHint(WindowHintBool.Decorated, true);

		glfwWindow = GLFW.CreateWindow(width, height, title, null, null);
		GLFW.DefaultWindowHints(); // reset hints

		// https://github.com/glfw/glfw/issues/1398
		// TODO looks like wayland requires a buffer to "draw" the window

		surface = new(vulkanManager.VulkanInstance, glfwWindow);

		// TODO get surface capable GPUs
		// TODO pick GPU. Manual or comparator method
		// TODO create logical gpu
	}

	public void Show() => GLFW.ShowWindow(glfwWindow);
	public void Hide() => GLFW.HideWindow(glfwWindow);

	public void RequestClose(bool force) {
		if (force) {
			this.shouldClose = true;
			return;
		}

		bool shouldClose = true;
		RequestCloseEvent?.Invoke(ref shouldClose);
		if (shouldClose) { this.shouldClose = true; }
	}

	internal void Cleanup() {
		surface.Cleanup();
		GLFW.DestroyWindow(glfwWindow);
	}

	public delegate bool RequestCloseDelegate(ref bool shouldShutdown);
}